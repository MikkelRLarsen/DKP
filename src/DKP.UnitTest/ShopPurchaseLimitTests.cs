using DKP.Application.Shop;
using DKP.Domain;
using Xunit;

namespace DKP.UnitTest;

public sealed class ShopPurchaseLimitTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Items_with_same_limit_key_share_total_and_active_limits()
    {
        var first = new ShopItem("tier-one", "Tier one", "", 1, 3, Now, purchaseLimitKey: "shared", maxActivePerUser: 2);
        var second = new ShopItem("tier-two", "Tier two", "", 1, 3, Now, purchaseLimitKey: "shared", maxActivePerUser: 2);
        var state = LedgerReplayState.Replay([
            Purchase(first, 1, Now.AddHours(-2), 1),
            Purchase(second, 1, Now.AddHours(-1), 2)]);

        var limits = ShopPurchaseLimitRules.Evaluate(state, first, [first, second], Now);

        Assert.Equal(2, limits.ActiveQuantity);
        Assert.Equal(2, limits.CountedQuantity);
        Assert.Equal(1, limits.RemainingQuantity);
    }

    [Fact]
    public void Period_limits_only_count_non_cancelled_purchases_in_the_period()
    {
        var item = new ShopItem("daily", "Daily", "", 1, 10, Now, maxPerDay: 2, maxPerWeek: 3, maxPerMonth: 4);
        var state = LedgerReplayState.Replay([Purchase(item, 1, Now.AddHours(-1), 1), Purchase(item, 1, Now.AddDays(-2), 2)]);

        var limits = ShopPurchaseLimitRules.Evaluate(state, item, [item], Now);

        Assert.Equal(1, limits.DailyRemaining);
        Assert.Equal(1, limits.WeeklyRemaining);
        Assert.Equal(2, limits.MonthlyRemaining);
        Assert.Equal(1, limits.RemainingQuantity);
    }

    [Fact]
    public void Active_limit_is_separate_from_total_limit()
    {
        var item = new ShopItem("active", "Active", "", 1, 5, Now, maxActivePerUser: 1);
        var state = LedgerReplayState.Replay([Purchase(item, 1, Now.AddHours(-1), 1)]);

        var limits = ShopPurchaseLimitRules.Evaluate(state, item, [item], Now);

        Assert.Equal(0, limits.RemainingQuantity);
    }

    [Fact]
    public void Configured_limit_is_inherited_and_shared_by_items()
    {
        var shared = new ShopPurchaseLimit("weekly", 4, null, null, 3, null, Now);
        var first = new ShopItem("first", "First", "", 1, 99, Now);
        var second = new ShopItem("second", "Second", "", 1, 99, Now);
        first.SetPurchaseLimit(shared.Id, Now);
        second.SetPurchaseLimit(shared.Id, Now);
        var state = LedgerReplayState.Replay([Purchase(first, 1, Now.AddHours(-1), 1), Purchase(second, 1, Now.AddHours(-1), 2)]);

        var limits = ShopPurchaseLimitRules.Evaluate(state, first, [first, second], [shared], Now);

        Assert.Equal(2, limits.RemainingQuantity);
        Assert.Equal(1, limits.WeeklyRemaining);
    }

    private static DkpEvent Purchase(ShopItem item, int quantity, DateTime occurredAtUtc, long sequence)
        => LedgerEvents.Create(UserId, UserId, sequence, Guid.NewGuid(), occurredAtUtc,
            new PurchasePlaced(Guid.NewGuid(), item.Id, item.Key, item.Name, quantity, item.Price, item.RollBonusValue));
}
