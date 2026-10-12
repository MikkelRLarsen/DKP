using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Application.Shop;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotShopQueries(IDbContextFactory<DkpDbContext> factory) : IBotShopQueries
{
    public async Task<IReadOnlyList<ShopItemDto>?> GetActiveItemsAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        return await ReadModels.ItemsAsync(db, true, ct);
    }

    public async Task<IReadOnlyList<ShopItemAvailabilityDto>?> GetAvailabilityAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;

        var state = await ReadModels.StateAsync(db, user.Id, ct);
        var items = await db.ShopItems.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).ToArrayAsync(ct);
        var requirements = await (from requirement in db.ShopItemAchievementRequirements.AsNoTracking()
                                  join achievement in db.AchievementDefinitions.AsNoTracking() on requirement.AchievementId equals achievement.Id
                                  select new { requirement.ShopItemId, AchievementId = achievement.Id, Dto = new ShopItemAchievementRequirementDto(achievement.Id, achievement.Name) })
            .ToArrayAsync(ct);
        var ownedAchievements = await db.UserAchievements.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .Select(x => x.AchievementId).ToHashSetAsync(ct);

        return items.Select(item =>
        {
            var itemRequirements = requirements.Where(x => x.ShopItemId == item.Id).ToArray();
            var purchases = state.Purchases.Values.Where(p => p.ShopItemId == item.Id && p.CancelledAtUtc is null).ToArray();
            var active = purchases.Where(p => !p.IsConsumed).Sum(p => p.Quantity);
            var used = purchases.Where(p => p.IsConsumed).Sum(p => p.Quantity);
            var counted = ShopPurchaseLimitRules.UsesActiveLimit(item) ? active : active + used;
            var rollBonusBlockedByAnotherTier = item.RollBonusValue is not null && state.HasActiveRollBonus() && active == 0;
            var remaining = rollBonusBlockedByAnotherTier ? 0 : Math.Max(0, item.MaxPerUser - counted);
            var requirementsMet = itemRequirements.All(x => ownedAchievements.Contains(x.AchievementId));
            return new ShopItemAvailabilityDto(item.Id, item.Key, item.Name, item.Description, item.Price, item.MaxPerUser, item.RollBonusValue, active, used, counted, remaining, remaining > 0 && requirementsMet, itemRequirements.Select(x => x.Dto).ToArray());
        }).ToArray();
    }

    public async Task<IReadOnlyList<ShopPurchaseDto>?> GetPurchasesAsync(string discordId, string? status = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        var purchases = await ReadModels.PurchasesAsync(db, user.Id, ct);
        return string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? purchases
            : purchases.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
