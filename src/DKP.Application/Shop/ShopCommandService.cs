using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.Shop;

public sealed class ShopCommandService(CommandContext context, IShopRepository catalog, IAchievementRepository achievementRepository, IEventLedgerRepository ledger, TimeProvider time) : IShopCommands
{
    private async Task<ShopItemDto> Dto(ShopItem i, CancellationToken ct) { var ids = await catalog.GetAchievementRequirementIdsAsync(i.Id, ct); return new(i.Id, i.Key, i.Name, i.Description, i.Price, i.MaxPerUser, i.IsActive, ids.Select(x => new ShopItemAchievementRequirementDto(x, "")).ToArray()); }
    private static void Validate(ShopItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Key) || input.Key.Trim().Length > 64 ||
            string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 128 ||
            input.Description is null || input.Description.Trim().Length > 500 ||
            input.Price < 0 || input.MaxPerUser <= 0)
            throw new ArgumentException("Invalid item: key/name, nonnegative price and positive maximum are required.");
    }
    public Task<ShopItemDto> CreateItemAsync(ShopItemInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var item = new ShopItem(input.Key.Trim(), input.Name.Trim(), input.Description.Trim(), input.Price, input.MaxPerUser, time.GetUtcNow().UtcDateTime);
            await catalog.AddItemAsync(item, ct);
            await ReplaceRequirementsAsync(item, input.AchievementIds, ct); return await Dto(item, ct);
        }, ct);
    public Task<ShopItemDto> UpdateItemAsync(Guid itemId, ShopItemInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var item = await catalog.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Item not found.");
            if (item.Key != input.Key.Trim()) throw new ArgumentException("Item key is immutable.");
            if (item.RollBonusValue != null && input.MaxPerUser != 1) throw new ArgumentException("RollBonus permits only one active unit across all tiers.");
            item.Update(input.Name.Trim(), input.Description.Trim(), input.Price, input.MaxPerUser, time.GetUtcNow().UtcDateTime);
            await ReplaceRequirementsAsync(item, input.AchievementIds, ct); return await Dto(item, ct);
        }, ct);
    public Task SetActiveAsync(Guid itemId, bool active, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var item = await catalog.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Item not found.");
            item.SetActive(active, time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);

    public Task<IReadOnlyList<ShopPurchaseDto>> PurchaseAsync(ShopPurchaseRequest request, CancellationToken ct = default)
        => context.ExecuteAsync<IReadOnlyList<ShopPurchaseDto>>([], false,
            actor => PurchaseInsideTransactionAsync(actor, [actor.Id], request.ShopItemId, request.Quantity, ct), ct);

    public Task<IReadOnlyList<ShopPurchaseDto>> PurchaseForUsersAsync(AdminShopPurchaseRequest request, CancellationToken ct = default)
    {
        var ids = CommandContext.Targets(request.TargetUserIds);
        return context.ExecuteAsync<IReadOnlyList<ShopPurchaseDto>>(ids, true,
            actor => PurchaseInsideTransactionAsync(actor, ids, request.ShopItemId, request.Quantity, ct), ct);
    }

    private async Task<IReadOnlyList<ShopPurchaseDto>> PurchaseInsideTransactionAsync(User actor, Guid[] ids, Guid itemId, int quantity, CancellationToken ct)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        var item = await catalog.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Item not found.");
        if (!item.IsActive) throw new InvalidOperationException("Item is inactive.");
        if (item.RollBonusValue != null && quantity != 1) throw new ArgumentException("Only one RollBonus unit can be purchased.");
        var cost = checked(item.Price * quantity);
        var users = new List<User>();
        foreach (var id in ids)
        {
            var target = await context.TargetAsync(id, ct);
            foreach (var achievementId in await catalog.GetAchievementRequirementIdsAsync(item.Id, ct))
            {
                var achievement = await achievementRepository.FindAsync(achievementId, ct);
                if (achievement is null || !achievement.IsActive || !await achievementRepository.HasActiveAsync(id, achievementId, ct))
                    throw new InvalidOperationException($"{target.DiscordName}: missing required achievement.");
            }
            var state = await ledger.GetStateAsync(id, ct);
            if (state.Balance < cost)
                throw new InvalidOperationException($"{target.DiscordName}: insufficient DKP ({cost} required).");
            var owned = ShopPurchaseLimitRules.OwnedQuantity(state, item);
            if ((long)owned + quantity > item.MaxPerUser)
                throw new InvalidOperationException($"{target.DiscordName}: maximum {item.MaxPerUser} for {item.Name} (already owns {owned}).");
            if (item.RollBonusValue != null && state.HasActiveRollBonus())
                throw new InvalidOperationException($"{target.DiscordName}: already has an active RollBonus.");
            users.Add(target);
        }
        var now = time.GetUtcNow().UtcDateTime;
        var operationId = Guid.NewGuid();
        var result = new List<ShopPurchaseDto>();
        foreach (var user in users)
        {
            var purchaseId = Guid.NewGuid();
            await ledger.PostAsync(user.Id, actor.Id, operationId, now,
                new PurchasePlaced(purchaseId, item.Id, item.Key, item.Name, quantity, item.Price, item.RollBonusValue), ct);
            result.Add(new(purchaseId, user.Id, user.DiscordName, null, item.Id, item.Name, quantity, cost, now, null));
        }
        return result;
    }

    private async Task ReplaceRequirementsAsync(ShopItem item, IReadOnlyList<Guid>? ids, CancellationToken ct)
    {
        var distinct = (ids ?? []).Where(x => x != Guid.Empty).Distinct().ToArray();
        foreach (var id in distinct)
        {
            var achievement = await achievementRepository.FindAsync(id, ct) ?? throw new KeyNotFoundException("Required achievement not found.");
            if (!achievement.IsActive) throw new InvalidOperationException("Inactive achievements cannot be shop requirements.");
        }
        await catalog.ReplaceAchievementRequirementsAsync(item.Id, distinct, ct);
    }

    public Task CancelAsync(Guid purchaseId, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            var state = await ledger.GetStateAsync(null, ct);
            var purchase = state.Purchases.Values.SingleOrDefault(x => x.PurchaseId == purchaseId) ?? throw new KeyNotFoundException("Purchase not found.");
            if (actor.Id != purchase.UserId && actor.Role != Domain.UserRole.Officer)
                throw new UnauthorizedAccessException("You can only cancel your own purchases.");
            await context.TargetAsync(purchase.UserId, ct);
            if (purchase.CancelledAtUtc != null) throw new InvalidOperationException("Purchase already cancelled.");
            if (purchase.IsConsumed) throw new InvalidOperationException("Used purchases cannot be cancelled.");
            await ledger.PostAsync(purchase.UserId, actor.Id, Guid.NewGuid(), time.GetUtcNow().UtcDateTime,
                new PurchaseCancelled(purchaseId, purchase.TotalDkpCost, $"Refund: {purchase.Quantity} x {purchase.ItemName}"), ct);
            return true;
        }, ct);

    public Task MarkUsedAsync(Guid purchaseId, CancellationToken ct = default)
        => context.ExecuteAsync<bool>([purchaseId], true, async officer =>
        {
            var state = await ledger.GetStateAsync(null, ct);
            var purchase = state.Purchases.Values.SingleOrDefault(x => x.PurchaseId == purchaseId) ?? throw new KeyNotFoundException("Purchase not found.");
            if (purchase.CancelledAtUtc is not null) throw new InvalidOperationException("Cancelled purchases cannot be marked used.");
            if (purchase.IsConsumed) throw new InvalidOperationException("Purchase is already used.");
            await ledger.PostAsync(purchase.UserId, officer.Id, Guid.NewGuid(), time.GetUtcNow().UtcDateTime, new PurchaseUsed(purchaseId, "Marked used by officer"), ct);
            return true;
        }, ct);

    public Task RevertUsedAsync(Guid purchaseId, CancellationToken ct = default)
        => context.ExecuteAsync<bool>([purchaseId], true, async officer =>
        {
            var state = await ledger.GetStateAsync(null, ct);
            var purchase = state.Purchases.Values.SingleOrDefault(x => x.PurchaseId == purchaseId) ?? throw new KeyNotFoundException("Purchase not found.");
            if (purchase.CancelledAtUtc is not null) throw new InvalidOperationException("Cancelled purchases cannot be reactivated.");
            if (!purchase.IsConsumed || purchase.ConsumptionEventId is not Guid useEventId) throw new InvalidOperationException("Purchase is not manually used or cannot be reactivated.");
            var events = await ledger.GetAllEventsAsync(ct);
            var useEntry = events.SingleOrDefault(x => x.Id == useEventId && x.UserId == purchase.UserId && x.EventType == nameof(PurchaseUsed)) ?? throw new InvalidOperationException("Purchase use event was not found.");
            await ledger.PostAsync(purchase.UserId, officer.Id, Guid.NewGuid(), time.GetUtcNow().UtcDateTime, new PurchaseUseReverted(purchaseId, useEntry.Id), ct);
            return true;
        }, ct);
}
