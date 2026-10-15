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

        var allItems = await db.ShopItems.AsNoTracking().ToArrayAsync(ct);
        var configuredLimits = await db.ShopPurchaseLimits.AsNoTracking().ToArrayAsync(ct);
        return items.Select(item =>
        {
            var itemRequirements = requirements.Where(x => x.ShopItemId == item.Id).ToArray();
            var limits = ShopPurchaseLimitRules.Evaluate(state, item, allItems, configuredLimits, DateTime.UtcNow);
            var rollBonusBlockedByAnotherTier = item.RollBonusValue is not null && state.HasActiveRollBonus() && limits.ActiveQuantity == 0;
            var remaining = rollBonusBlockedByAnotherTier || !ShopPurchaseLimitRules.IsEnabled(item, configuredLimits) ? 0 : limits.RemainingQuantity;
            var requirementsMet = itemRequirements.All(x => ownedAchievements.Contains(x.AchievementId));
            var inherited = item.PurchaseLimitId is Guid limitId ? configuredLimits.SingleOrDefault(x => x.Id == limitId) : null;
            return new ShopItemAvailabilityDto(item.Id, item.Key, item.Name, item.Description, item.Price, inherited?.MaxPerUser ?? item.MaxPerUser, item.RollBonusValue, limits.ActiveQuantity, limits.UsedQuantity, limits.CountedQuantity, remaining, remaining > 0 && requirementsMet, itemRequirements.Select(x => x.Dto).ToArray(), inherited?.SharedKey ?? item.PurchaseLimitKey, inherited?.MaxActivePerUser ?? item.MaxActivePerUser, inherited?.MaxPerDay ?? item.MaxPerDay, inherited?.MaxPerWeek ?? item.MaxPerWeek, inherited?.MaxPerMonth ?? item.MaxPerMonth, limits.DailyRemaining, limits.WeeklyRemaining, limits.MonthlyRemaining);
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
