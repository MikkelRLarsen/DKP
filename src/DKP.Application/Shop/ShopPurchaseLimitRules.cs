using DKP.Domain;

namespace DKP.Application.Shop;

/// <summary>Central purchase-limit calculation shared by web, bot and commands.</summary>
public static class ShopPurchaseLimitRules
{
    public static bool UsesActiveLimit(ShopItem item)
        => item.Key == "soft-reserve" || item.RollBonusValue is not null;

    public static string GroupKey(ShopItem item)
        => item.PurchaseLimitId is Guid id ? $"limit:{id:N}" : string.IsNullOrWhiteSpace(item.PurchaseLimitKey) ? item.Key : item.PurchaseLimitKey.Trim();

    public static int OwnedQuantity(LedgerReplayState state, ShopItem item)
        => Evaluate(state, item, [item], DateTime.UtcNow).CountedQuantity;

    public static bool IsEnabled(ShopItem item, IReadOnlyCollection<ShopPurchaseLimit> configuredLimits)
        => item.PurchaseLimitId is not Guid id || configuredLimits.SingleOrDefault(x => x.Id == id)?.IsActive == true;

    public static ShopLimitSnapshot Evaluate(LedgerReplayState state, ShopItem item, IReadOnlyCollection<ShopItem> allItems, DateTime nowUtc)
        => Evaluate(state, item, allItems, [], nowUtc);

    public static ShopLimitSnapshot Evaluate(LedgerReplayState state, ShopItem item, IReadOnlyCollection<ShopItem> allItems, IReadOnlyCollection<ShopPurchaseLimit> configuredLimits, DateTime nowUtc)
    {
        var groupKey = GroupKey(item);
        var groupItems = allItems.Where(x => GroupKey(x) == groupKey).ToArray();
        var itemIds = groupItems.Select(x => x.Id).ToHashSet();
        var purchases = state.Purchases.Values.Where(p => itemIds.Contains(p.ShopItemId) && p.CancelledAtUtc is null).ToArray();
        var active = purchases.Where(x => !x.IsConsumed).Sum(x => x.Quantity);
        var used = purchases.Where(x => x.IsConsumed).Sum(x => x.Quantity);
        var limit = item.PurchaseLimitId is Guid limitId ? configuredLimits.SingleOrDefault(x => x.Id == limitId) : null;
        var maxPerUser = limit?.MaxPerUser ?? (item.MaxPerUser > 0 ? item.MaxPerUser : null);
        var maxActivePerUser = limit?.MaxActivePerUser ?? item.MaxActivePerUser;
        var maxPerDay = limit?.MaxPerDay ?? item.MaxPerDay;
        var maxPerWeek = limit?.MaxPerWeek ?? item.MaxPerWeek;
        var maxPerMonth = limit?.MaxPerMonth ?? item.MaxPerMonth;
        var counted = UsesActiveLimit(item) ? active : active + used;
        var totalRemaining = maxPerUser is int max ? Math.Max(0, max - counted) : int.MaxValue;
        var activeRemaining = maxActivePerUser is int maxActive ? Math.Max(0, maxActive - active) : int.MaxValue;
        var dailyRemaining = PeriodRemaining(purchases, nowUtc.Date, maxPerDay);
        var weeklyRemaining = PeriodRemaining(purchases, StartOfWeek(nowUtc), maxPerWeek);
        var monthlyRemaining = PeriodRemaining(purchases, new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc), maxPerMonth);
        var remaining = new[] { totalRemaining, activeRemaining, dailyRemaining ?? int.MaxValue, weeklyRemaining ?? int.MaxValue, monthlyRemaining ?? int.MaxValue }.Min();
        return new ShopLimitSnapshot(active, used, counted, remaining, dailyRemaining, weeklyRemaining, monthlyRemaining);
    }

    private static int? PeriodRemaining(IReadOnlyCollection<LedgerPurchaseState> purchases, DateTime startUtc, int? maximum)
    {
        if (maximum is not int max) return null;
        var quantity = purchases.Where(x => x.CreatedAtUtc >= startUtc).Sum(x => x.Quantity);
        return Math.Max(0, max - quantity);
    }

    private static DateTime StartOfWeek(DateTime value)
    {
        var date = value.Date;
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return DateTime.SpecifyKind(date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    }
}

public sealed record ShopLimitSnapshot(int ActiveQuantity, int UsedQuantity, int CountedQuantity, int RemainingQuantity,
    int? DailyRemaining, int? WeeklyRemaining, int? MonthlyRemaining);
