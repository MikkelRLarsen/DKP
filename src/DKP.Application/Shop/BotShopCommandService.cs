using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Shop;

public sealed class BotShopCommandService(
    IUserRepository users,
    IShopRepository catalog,
    IAchievementRepository achievements,
    IEventLedgerRepository ledger,
    ICommandUnitOfWork unitOfWork,
    TimeProvider time) : IBotShopCommands
{
    public Task<ShopPurchaseDto> PurchaseAsync(string discordId, ShopPurchaseRequest request, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            if (request.Quantity <= 0) throw new ArgumentException("Quantity must be positive.");

            var item = await catalog.FindItemAsync(request.ShopItemId, ct)
                ?? throw new KeyNotFoundException("Shop item not found.");
            if (!item.IsActive) throw new InvalidOperationException("Shop item is inactive.");
            if (item.RollBonusValue is not null && request.Quantity != 1)
                throw new ArgumentException("Only one RollBonus unit can be purchased.");

            var cost = checked(item.Price * request.Quantity);
            foreach (var achievementId in await catalog.GetAchievementRequirementIdsAsync(item.Id, ct))
            {
                var achievement = await achievements.FindAsync(achievementId, ct);
                if (achievement is null || !achievement.IsActive || !await achievements.HasActiveAsync(actor.Id, achievementId, ct))
                    throw new InvalidOperationException("You do not meet the requirements for this shop item.");
            }

            var state = await ledger.GetStateAsync(actor.Id, ct);
            if (state.Balance < cost) throw new InvalidOperationException($"Insufficient DKP ({cost} required).");
            if ((long)state.ActiveQuantity(item.Id) + request.Quantity > item.MaxPerUser)
                throw new InvalidOperationException($"Maximum {item.MaxPerUser} for {item.Name} has been reached.");
            if (item.RollBonusValue is not null && state.HasActiveRollBonus())
                throw new InvalidOperationException("You already have an active RollBonus.");

            var now = time.GetUtcNow().UtcDateTime;
            var purchaseId = Guid.NewGuid();
            await ledger.PostAsync(actor.Id, actor.Id, Guid.NewGuid(), now,
                new PurchasePlaced(purchaseId, item.Id, item.Key, item.Name, request.Quantity, item.Price, item.RollBonusValue), ct);
            return new ShopPurchaseDto(purchaseId, actor.Id, actor.DiscordName, null, item.Id, item.Name,
                request.Quantity, cost, now, null);
        }, ct);

    public Task<bool> CancelAsync(string discordId, Guid purchaseId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var actor = await ActiveUserAsync(discordId, ct);
            var state = await ledger.GetStateAsync(actor.Id, ct);
            var purchase = state.Purchases.Values.SingleOrDefault(x => x.PurchaseId == purchaseId);
            if (purchase is null) throw new KeyNotFoundException("Purchase not found.");
            if (purchase.UserId != actor.Id) throw new UnauthorizedAccessException("You can only cancel your own purchases.");
            if (purchase.CancelledAtUtc is not null) throw new InvalidOperationException("Purchase already cancelled.");
            if (purchase.IsConsumed) throw new InvalidOperationException("Used purchases cannot be cancelled.");

            await ledger.PostAsync(actor.Id, actor.Id, Guid.NewGuid(), time.GetUtcNow().UtcDateTime,
                new PurchaseCancelled(purchaseId, purchase.TotalDkpCost, $"Refund: {purchase.Quantity} x {purchase.ItemName}"), ct);
            return true;
        }, ct);

    private async Task<User> ActiveUserAsync(string discordId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("An active member is required.");
        var user = await users.FindByDiscordIdAsync(discordId, ct);
        return user is null || user.IsBlocked
            ? throw new UnauthorizedAccessException("An active member is required.")
            : user;
    }
}
