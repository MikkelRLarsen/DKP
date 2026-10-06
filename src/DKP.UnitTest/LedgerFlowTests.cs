using DKP.Application.DkpTransactions;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace DKP.UnitTest;

public sealed class LedgerFlowTests : DatabaseTest
{
    [Fact]
    public async Task All_views_use_same_balance_and_actual_event_ids()
    {
        var admin = As("officer");
        var member = As("member");
        var credit = await admin.Dkp.AddAsync(new(Member.Id, 100, "Credit"));
        await admin.Dkp.RemoveAsync(new(Member.Id, 20, "Debit"));
        var preset = await admin.Presets.CreateAsync(new("Attendance", 5, "Attended", 2));
        var award = await admin.Presets.ApplyAsync(Member.Id, preset.Id);
        var purchase = Assert.Single(await member.Shop.PurchaseAsync(new(SoftReserveId, 2)));
        await member.Shop.CancelAsync(purchase.Id);
        var own = await new DkpQueries(member.Queries).GetHistoryAsync();
        Assert.NotNull(own);
        Assert.Equal(85, own.Balance.Amount);
        Assert.Equal(5, own.Transactions.Count);
        Assert.Equal(85, own.Transactions.Sum(x => x.Amount));
        Assert.Equal(credit.Id, own.Transactions.Last().Id);
        Assert.Equal(85, (await new AccountQueries(member.Queries).GetDashboardAsync())!.DkpBalance);
        Assert.Equal(85, (await new GuildMemberQueries(member.Queries).GetAllAsync()).Single(x => x.UserId == Member.Id).DkpBalance);
        Assert.Equal(85, (await new PlayerDetailsQueries(member.Queries).GetAsync(Member.Id))!.DkpHistory.Balance.Amount);
        Assert.Equal(own.Transactions, (await new DkpQueries(member.Queries).GetPlayerHistoryAsync(Member.Id))!.Transactions);
        await using var db = Factory.CreateDbContext();
        Assert.True(await db.DkpEvents.AnyAsync(x => x.Id == award.Id));
        var usage = await db.DkpAwardPresetApplications.SingleAsync();
        Assert.Equal(award.Id, usage.DkpEventId);
        Assert.Equal(Officer.Id, usage.AppliedByUserId);
        Assert.Equal(own.Transactions.First().Id, (await db.DkpBalanceProjections.FindAsync(Member.Id))!.LastEventId);
    }

    [Fact]
    public async Task Bulk_deduplicates_users_and_shares_operation_not_event_id()
    {
        var result = await As("officer").Dkp.AddManyAsync(new([Member.Id, Other.Id, Member.Id], 7, "Bulk"));
        Assert.Equal(2, result.Count);
        Assert.Equal(7, await BalanceAsync(Member.Id));
        Assert.Equal(7, await BalanceAsync(Other.Id));
        await using var db = Factory.CreateDbContext();
        var events = await db.DkpEvents.ToArrayAsync();
        Assert.Single(events.Select(x => x.CorrelationId).Distinct());
        Assert.Equal(2, events.Select(x => x.Id).Distinct().Count());
        Assert.All(events, x => Assert.Contains(result, r => r.Id == x.Id));
    }

    [Fact]
    public async Task Invalid_bulk_has_no_partial_postings()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => As("officer").Dkp.AddManyAsync(new([Member.Id, Guid.NewGuid()], 9, "Invalid target")));
        Assert.Equal(0, await BalanceAsync(Member.Id));
        await As("officer").Blocks.BlockAsync(new(Other.Id, "test"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => As("officer").Dkp.AddManyAsync(new([Member.Id, Other.Id], 9, "Blocked target")));
        Assert.Contains(Other.DiscordName, error.Message);
        Assert.Equal(0, await BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Failure_after_first_post_rolls_back_and_same_service_can_be_reused()
    {
        var rig = As("officer");
        var fault = new FailSecondPost(rig.Ledger);
        var service = new DkpTransactionCommandService(rig.Context, fault, new FixedTime());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddManyAsync(new([Member.Id, Other.Id], 3, "atomic")));
        Assert.Equal(0, await BalanceAsync(Member.Id));
        Assert.Equal(0, await BalanceAsync(Other.Id));
        await using (var db = Factory.CreateDbContext()) Assert.Equal(0, await db.DkpEvents.CountAsync());
        await service.AddAsync(new(Member.Id, 4, "retry as complete operation"));
        Assert.Equal(4, await BalanceAsync(Member.Id));
    }

    [Theory]
    [InlineData(0, "Reason")]
    [InlineData(-1, "Reason")]
    [InlineData(1, " ")]
    public async Task Invalid_manual_inputs_rejected(int amount, string reason)
        => await Assert.ThrowsAsync<ArgumentException>(() => As("officer").Dkp.AddAsync(new(Member.Id, amount, reason)));

    [Fact]
    public async Task Officer_can_make_negative_balance_and_reason_is_trimmed()
    {
        var entry = await As("officer").Dkp.RemoveAsync(new(Member.Id, 50, "  correction  "));
        Assert.Equal(-50, await BalanceAsync(Member.Id));
        Assert.Equal("correction", entry.Reason);
        await Assert.ThrowsAsync<ArgumentException>(() => As("officer").Dkp.AddAsync(new(Member.Id, 1, new string('x', 501))));
    }

    [Fact]
    public async Task Own_history_does_not_include_other_players_and_missing_player_is_null()
    {
        await FundAsync(Other.Id);
        var query = new DkpQueries(As("member").Queries);
        Assert.Empty((await query.GetHistoryAsync())!.Transactions);
        Assert.Equal(0, (await query.GetHistoryAsync())!.Balance.Amount);
        Assert.Single((await query.GetPlayerHistoryAsync(Other.Id))!.Transactions);
        Assert.Null(await query.GetPlayerHistoryAsync(Guid.NewGuid()));
        Assert.Null(await new PlayerDetailsQueries(As("member").Queries).GetAsync(Guid.NewGuid()));
    }

    private sealed class FailSecondPost(IEventLedgerRepository inner) : IEventLedgerRepository
    {
        private int calls;
        public Task<int> GetBalanceAsync(Guid id, CancellationToken ct = default) => inner.GetBalanceAsync(id, ct);
        public Task<int> GetActiveQuantityAsync(Guid id, Guid item, CancellationToken ct = default) => inner.GetActiveQuantityAsync(id, item, ct);
        public Task<bool> HasActiveRollBonusAsync(Guid id, CancellationToken ct = default) => inner.HasActiveRollBonusAsync(id, ct);
        public Task<ShopPurchaseProjection?> GetPurchaseAsync(Guid id, CancellationToken ct = default) => inner.GetPurchaseAsync(id, ct);
        public Task<DkpEvent> PostAsync(Guid user, Guid actor, Guid operation, DateTime now, ILedgerPayload payload, CancellationToken ct = default)
        {
            if (++calls == 2) throw new InvalidOperationException("Injected mid-operation failure");
            return inner.PostAsync(user, actor, operation, now, payload, ct);
        }
    }
}
