using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace DKP.UnitTest;

public sealed class ReplayAndActivityTests : DatabaseTest
{
    [Fact]
    public async Task Replay_is_repeatable_with_identical_timestamps_and_keeps_ids_usage_and_snapshots()
    {
        var admin = As("officer");
        var member = As("member");
        await FundAsync(Member.Id);
        await FundAsync(Other.Id);
        var preset = await admin.Presets.CreateAsync(new("Raid", 10, "Attendance", 2));
        await admin.Presets.ApplyManyAsync(preset.Id, [Member.Id, Other.Id]);
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 2)));
        await member.Shop.CancelAsync(purchase.Id);
        await member.Shop.PurchaseAsync(new(BonusId(30), 1));
        var history = (await new DkpQueries(member.Queries).GetHistoryAsync())!;
        var beforeActivity = await new GuildActivityQueries(member.Queries).GetAsync(new(Take: 200));
        await admin.Shop.UpdateItemAsync(BonusId(30), new("roll-bonus-30", "Renamed", "", 80, 1));
        var rebuilder = new EventProjectionRebuilder(admin.Factory);
        await rebuilder.RebuildAsync();
        await rebuilder.RebuildAsync();
        var after = (await new DkpQueries(member.Queries).GetHistoryAsync())!;
        Assert.Equal(history.Balance, after.Balance);
        Assert.Equal(history.Transactions, after.Transactions);
        Assert.Equal(beforeActivity.Items, (await new GuildActivityQueries(member.Queries).GetAsync(new(Take: 200))).Items);
        Assert.Equal(30, (await new ShopPurchaseQueries(member.Queries).GetActiveOverviewAsync())!.RollBonus);
        await using var db = Factory.CreateDbContext();
        var presetEvents = (await db.DkpEvents.Where(x => x.EventType == "DkpPosted").ToArrayAsync()).Where(x => x.Payload.Contains(preset.Id.ToString())).ToArray();
        Assert.Equal(2, presetEvents.Length);
        Assert.Single(await db.DkpEvents.Select(x => x.OccurredAtUtc).Distinct().ToListAsync());
        var bonusEvents = await db.DkpEvents.ToArrayAsync();
        Assert.Contains(bonusEvents, x => x.Payload.Contains("RollBonus 30"));
    }

    [Fact]
    public async Task Invalid_event_mid_replay_rolls_back_every_projection()
    {
        await FundAsync(Member.Id);
        var rig = As("member");
        await rig.Shop.PurchaseAsync(new(SoftReserveId, 1));
        var before = (await new DkpQueries(rig.Queries).GetHistoryAsync())!;
        await using (var db = Factory.CreateDbContext())
        {
            db.DkpEvents.Add(new("UserLedger", Member.Id, 3, "UnknownFutureEvent", Member.Id, Officer.Id,
                DateTime.UtcNow, Guid.NewGuid(), "{}"));
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EventProjectionRebuilder(rig.Factory).RebuildAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DkpQueries(rig.Queries).GetHistoryAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ShopQueries(rig.Queries).GetPurchasesAsync());
    }

    [Fact]
    public async Task Duplicate_cancellation_event_fails_replay_without_double_refund()
    {
        await FundAsync(Member.Id);
        var member = As("member");
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 1)));
        await member.Shop.CancelAsync(purchase.Id);
        await using (var db = Factory.CreateDbContext())
        {
            db.DkpEvents.Add(LedgerEvents.Create(Member.Id, Officer.Id, 4, Guid.NewGuid(), DateTime.UtcNow,
                new PurchaseCancelled(purchase.Id, 10, "Duplicate refund")));
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EventProjectionRebuilder(member.Factory).RebuildAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Replay_and_concurrent_command_are_serialized()
    {
        await FundAsync(Member.Id);
        var replay = new EventProjectionRebuilder(As("officer").Factory).RebuildAsync();
        var purchase = As("member").Shop.PurchaseAsync(new(SoftReserveId, 2));
        await Task.WhenAll(replay, purchase);
        Assert.Equal(80, await BalanceAsync(Member.Id));
        Assert.Single(await new ShopQueries(As("member").Queries).GetPurchasesAsync());
    }

    [Fact]
    public async Task Activity_has_one_row_per_action_and_pages_filters_stably()
    {
        await FundAsync(Member.Id);
        await FundAsync(Other.Id);
        var member = As("member");
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 2)));
        await member.Shop.CancelAsync(purchase.Id);
        var query = new GuildActivityQueries(member.Queries);
        var all = await query.GetAsync(new());
        Assert.Equal(4, all.TotalCount);
        Assert.Single(all.Items, x => x.Action == "purchase");
        Assert.Single(all.Items, x => x.Action == "cancellation");
        var firstPage = await query.GetAsync(new(Take: 2));
        var secondPage = await query.GetAsync(new(Skip: 2, Take: 2));
        Assert.Equal(all.Items.Select(x => x.Id), firstPage.Items.Concat(secondPage.Items).Select(x => x.Id));
        Assert.Equal(all.Items.Select(x => x.Id), (await query.GetAsync(new())).Items.Select(x => x.Id));
        Assert.Equal(3, (await query.GetAsync(new(UserId: Member.Id))).TotalCount);
        var bought = Assert.Single((await query.GetAsync(new(Action: "purchase"))).Items);
        Assert.Equal(-20, bought.Amount);
        Assert.Equal(2, bought.Quantity);
        Assert.Equal("Soft Reserve", bought.ItemName);
        Assert.Equal(Member.DiscordName, bought.ActorName);
        Assert.Empty((await query.GetAsync(new(FromUtc: new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)))).Items);
        var ascending = await query.GetAsync(new(Descending: false));
        Assert.Equal(all.Items.Reverse().Select(x => x.Id), ascending.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task Events_cannot_be_modified_or_deleted_through_tracked_or_set_based_persistence()
    {
        await FundAsync(Member.Id);
        await using (var db = Factory.CreateDbContext())
        {
            var entry = await db.DkpEvents.SingleAsync();
            db.Entry(entry).Property(x => x.Payload).CurrentValue = "{}";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = Factory.CreateDbContext())
        {
            db.DkpEvents.Remove(await db.DkpEvents.SingleAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = Factory.CreateDbContext())
            await Assert.ThrowsAsync<PostgresException>(() => db.DkpEvents.ExecuteDeleteAsync());
    }

    [Fact]
    public async Task New_baseline_has_no_pending_changes_and_expected_seed_and_relations()
    {
        await using var db = Factory.CreateDbContext();
        Assert.Equal(4, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(0, (await db.GuildSettings.SingleAsync()).DefaultReserveLimit);
        var sr = await db.ShopItems.SingleAsync(x => x.Key == "soft-reserve");
        Assert.Equal(10, sr.Price);
        Assert.Equal(2, sr.MaxPerUser);
        Assert.Equal(new[] { 10, 30, 60, 120 }, await db.ShopItems.Where(x => x.RollBonusValue != null).OrderBy(x => x.RollBonusValue).Select(x => x.Price).ToArrayAsync());
        Assert.DoesNotContain(db.Model.GetEntityTypes(), e => new[] { "DkpTransaction", "SoftReservePurchase", "ShopPurchase" }.Contains(e.ClrType.Name));
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(User))!.GetProperties(), p => p.Name == "RollBonus");
        Assert.Null(db.Model.FindEntityType("DkpAwardPresetApplication"));
    }
}
