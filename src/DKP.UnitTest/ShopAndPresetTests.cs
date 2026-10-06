using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace DKP.UnitTest;

public sealed class ShopAndPresetTests : DatabaseTest
{
    [Fact]
    public async Task Catalog_is_price_and_limit_authority_and_purchase_snapshot_survives_edits()
    {
        await FundAsync(Member.Id);
        var admin = As("officer");
        await admin.Shop.UpdateItemAsync(SoftReserveId, new("soft-reserve", "SR revised", "", 7, 3));
        var member = As("member");
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 3)));
        Assert.Equal(21, purchase.TotalDkpCost);
        Assert.Equal(79, await BalanceAsync(Member.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => member.Shop.PurchaseAsync(new(SoftReserveId, 1)));
        await admin.Shop.UpdateItemAsync(SoftReserveId, new("soft-reserve", "SR new price", "", 50, 2));
        var history = Assert.Single(await new ShopQueries(member.Queries).GetPurchasesAsync());
        Assert.Equal("SR revised", history.ItemName);
        Assert.Equal(21, history.TotalDkpCost);
        var overview = (await new ShopPurchaseQueries(member.Queries).GetActiveOverviewAsync())!;
        Assert.Equal(3, overview.Items.Single().Quantity);
        Assert.Equal(2, overview.Items.Single().MaxPerUser);
        Assert.Equal(0, overview.Items.Single().RemainingQuantity);
        await member.Shop.CancelAsync(purchase.Id);
        Assert.Equal(100, await BalanceAsync(Member.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => member.Shop.CancelAsync(purchase.Id));
        Assert.Equal(100, await BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Concurrent_purchases_enforce_catalog_limit_and_balance()
    {
        await FundAsync(Member.Id, 20);
        async Task<bool> Buy()
        {
            try { await As("member").Shop.PurchaseAsync(new(SoftReserveId, 2)); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(Buy(), Buy());
        Assert.Single(results, x => x);
        Assert.Equal(0, await BalanceAsync(Member.Id));
        Assert.Single(await new ShopQueries(As("member").Queries).GetPurchasesAsync());
    }

    [Fact]
    public async Task RollBonus_is_single_across_tiers_and_cancellation_releases_it()
    {
        await FundAsync(Member.Id, 300);
        var member = As("member");
        await Assert.ThrowsAsync<ArgumentException>(() => member.Shop.PurchaseAsync(new(BonusId(10), 2)));
        var first = Assert.Single(await member.Shop.PurchaseAsync(new(BonusId(30), 1)));
        Assert.Equal(30, (await new ShopPurchaseQueries(member.Queries).GetActiveOverviewAsync())!.RollBonus);
        await Assert.ThrowsAsync<InvalidOperationException>(() => member.Shop.PurchaseAsync(new(BonusId(10), 1)));
        await member.Shop.CancelAsync(first.Id);
        Assert.Equal(0, (await new ShopPurchaseQueries(member.Queries).GetActiveOverviewAsync())!.RollBonus);
        await member.Shop.PurchaseAsync(new(BonusId(40), 1));
        Assert.Equal(40, (await new LootReserveQueries(As("officer").Queries).GetMembersAsync()).Single(x => x.UserId == Member.Id).RollBonus);
    }

    [Fact]
    public async Task Multi_user_purchase_is_all_or_nothing_with_officer_attribution()
    {
        await FundAsync(Member.Id, 30);
        var admin = As("officer");
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Shop.PurchaseForUsersAsync(new(SoftReserveId, 1, [Member.Id, Other.Id])));
        Assert.Equal(30, await BalanceAsync(Member.Id));
        Assert.Empty(await new ShopQueries(admin.Queries).GetAllPurchasesAsync());
        await FundAsync(Other.Id, 30);
        var results = await admin.Shop.PurchaseForUsersAsync(new(SoftReserveId, 2, [Member.Id, Other.Id, Member.Id]));
        Assert.Equal(2, results.Count);
        Assert.Equal(10, await BalanceAsync(Member.Id));
        Assert.Equal(10, await BalanceAsync(Other.Id));
        await using var db = Factory.CreateDbContext();
        Assert.All(await db.ShopPurchaseProjections.ToListAsync(), p => Assert.Equal(Officer.Id, p.CreatedByUserId));
        await admin.Shop.CancelAsync(results[0].Id);
        Assert.Equal(30, await BalanceAsync(results[0].UserId));
    }

    [Fact]
    public async Task Catalog_create_deactivate_and_maximum_is_per_item_id()
    {
        var admin = As("officer");
        var first = await admin.Shop.CreateItemAsync(new("first", "Same Name", "Description", 3, 1));
        var second = await admin.Shop.CreateItemAsync(new("second", "Same Name", "Description", 4, 1));
        await FundAsync(Member.Id);
        var member = As("member");
        await member.Shop.PurchaseAsync(new(first.Id, 1));
        await member.Shop.PurchaseAsync(new(second.Id, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => member.Shop.PurchaseAsync(new(first.Id, 1)));
        await admin.Shop.SetActiveAsync(second.Id, false);
        Assert.DoesNotContain(await new ShopQueries(member.Queries).GetActiveItemsAsync(), x => x.Id == second.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => member.Shop.PurchaseAsync(new(second.Id, 1)));
        await Assert.ThrowsAsync<ArgumentException>(() => admin.Shop.UpdateItemAsync(first.Id, new("different-key", "Name", "", 3, 1)));
        Assert.Equal(2, (await new ShopPurchaseQueries(member.Queries).GetActiveOverviewAsync())!.Items.Count(x => x.Quantity > 0));
    }

    [Fact]
    public async Task Presets_enforce_lifetime_limit_after_edits_and_replay()
    {
        var admin = As("officer");
        var preset = await admin.Presets.CreateAsync(new("Quest", 10, "Quest complete", 3));
        var result = await admin.Presets.ApplyManyAsync(preset.Id, [Member.Id, Member.Id, Other.Id]);
        Assert.Equal(2, result.Count);
        await admin.Presets.ApplyAsync(Member.Id, preset.Id);
        await admin.Presets.UpdateAsync(preset.Id, new("Quest edited", 20, "Changed reason", 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Presets.ApplyAsync(Member.Id, preset.Id));
        Assert.Equal(20, await BalanceAsync(Member.Id));
        var queries = new DkpPresetQueries(admin.Queries);
        Assert.Equal(2, (await queries.GetUsageAsync(Member.Id, preset.Id)).Applications);
        Assert.Empty(await new DkpPresetQueries(As("member").Queries).GetAvailableSourcesAsync());
        await new DKP.Infrastructure.Persistence.EventProjectionRebuilder(admin.Session).RebuildAsync();
        Assert.Equal(2, (await queries.GetUsageAsync(Member.Id, preset.Id)).Applications);
        Assert.All((await new DkpQueries(As("member").Queries).GetHistoryAsync())!.Transactions, x => Assert.Equal("Quest complete", x.Reason));
        await admin.Presets.UpdateAsync(preset.Id, new("Quest edited", 20, "Changed reason", 5));
        await admin.Presets.SetActiveAsync(preset.Id, false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Presets.ApplyAsync(Member.Id, preset.Id));
    }

    [Fact]
    public async Task Concurrent_preset_applications_cannot_exceed_lifetime_limit()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Once", 10, "Once only", 1));
        async Task<bool> Apply()
        {
            try { await As("officer").Presets.ApplyAsync(Member.Id, preset.Id); return true; }
            catch (InvalidOperationException) { return false; }
        }
        Assert.Single(await Task.WhenAll(Apply(), Apply()), x => x);
        Assert.Equal(10, await BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Bulk_preset_rejects_entire_group_if_one_player_is_at_limit()
    {
        var admin = As("officer");
        var preset = await admin.Presets.CreateAsync(new("Once", 10, "Reason", 1));
        await admin.Presets.ApplyAsync(Other.Id, preset.Id);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => admin.Presets.ApplyManyAsync(preset.Id, [Member.Id, Other.Id]));
        Assert.Contains(Other.DiscordName, error.Message);
        Assert.Equal(0, await BalanceAsync(Member.Id));
    }
}
