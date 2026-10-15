using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Application.Shop;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopPurchaseQueries(QuerySession session) : IShopPurchaseQueries
{
    public Task<IReadOnlyList<ShopItemAvailabilityDto>> GetAvailabilityAsync(bool includeInactive = false, CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<ShopItemAvailabilityDto>>(false, async (db, actor) =>
    {
        var state = await ReadModels.StateAsync(db, actor.Id, ct);
        var itemsQuery = db.ShopItems.AsQueryable();
        if (!includeInactive) itemsQuery = itemsQuery.Where(x => x.IsActive);
        var items = await itemsQuery.OrderBy(x => x.Name).ThenBy(x => x.Id).ToArrayAsync(ct);
        var allItems = await db.ShopItems.AsNoTracking().ToArrayAsync(ct);
        var configuredLimits = await db.ShopPurchaseLimits.AsNoTracking().ToArrayAsync(ct);
        var requirements = await (from requirement in db.ShopItemAchievementRequirements
                                  join achievement in db.AchievementDefinitions on requirement.AchievementId equals achievement.Id
                                  select new { requirement.ShopItemId, Dto = new ShopItemAchievementRequirementDto(achievement.Id, achievement.Name) })
            .ToArrayAsync(ct);
        return items.Select(item =>
        {
            var limits = ShopPurchaseLimitRules.Evaluate(state, item, allItems, configuredLimits, DateTime.UtcNow);
            var rollBonusBlockedByAnotherTier = item.RollBonusValue is not null && state.HasActiveRollBonus() && limits.ActiveQuantity == 0;
            var remaining = rollBonusBlockedByAnotherTier || !ShopPurchaseLimitRules.IsEnabled(item, configuredLimits) ? 0 : limits.RemainingQuantity;
            var inherited = item.PurchaseLimitId is Guid limitId ? configuredLimits.SingleOrDefault(x => x.Id == limitId) : null;
            return new ShopItemAvailabilityDto(item.Id, item.Key, item.Name, item.Description, item.Price, inherited?.MaxPerUser ?? item.MaxPerUser, item.RollBonusValue, limits.ActiveQuantity, limits.UsedQuantity, limits.CountedQuantity, remaining, remaining > 0, requirements.Where(x => x.ShopItemId == item.Id).Select(x => x.Dto).ToArray(), inherited?.SharedKey ?? item.PurchaseLimitKey, inherited?.MaxActivePerUser ?? item.MaxActivePerUser, inherited?.MaxPerDay ?? item.MaxPerDay, inherited?.MaxPerWeek ?? item.MaxPerWeek, inherited?.MaxPerMonth ?? item.MaxPerMonth, limits.DailyRemaining, limits.WeeklyRemaining, limits.MonthlyRemaining);
        }).ToArray();
    }, ct);

    public Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(CancellationToken ct = default) => session.ReadAsync<ActivePurchaseOverviewDto?>(false, async (db, actor) =>
    {
        var state = await ReadModels.StateAsync(db, actor.Id, ct);
        var items = await db.ShopItems.OrderBy(i => i.Name).ThenBy(i => i.Id).ToArrayAsync(ct);
        var configuredLimits = await db.ShopPurchaseLimits.AsNoTracking().ToArrayAsync(ct);
        var result = items.Where(i => i.Key == "soft-reserve" || state.Purchases.Values.Any(p => p.ShopItemId == i.Id && p.CancelledAtUtc is null && !p.IsConsumed)).Select(i =>
        {
            var owned = state.Purchases.Values.Where(p => p.ShopItemId == i.Id && p.CancelledAtUtc is null && !p.IsConsumed).ToArray();
            var quantity = owned.Sum(p => p.ActiveQuantity);
            var limits = ShopPurchaseLimitRules.Evaluate(state, i, items, configuredLimits, DateTime.UtcNow);
            return new ActivePurchaseItemDto(i.Key, i.Name, quantity, i.MaxPerUser, limits.RemainingQuantity, i.Price, owned.FirstOrDefault()?.RollBonusValue ?? i.RollBonusValue);
        }).ToArray();
        return new ActivePurchaseOverviewDto(result, state.Purchases.Values.Where(p => p.CancelledAtUtc is null && !p.IsConsumed).Sum(p => p.RollBonusValue ?? 0));
    }, ct);
}
