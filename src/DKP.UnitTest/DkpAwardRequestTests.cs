using DKP.Facade.Contracts;
using DKP.Infrastructure.Queries;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
using RequestStatus = DKP.Facade.Contracts.DkpAwardRequestStatus;
using Xunit;

namespace DKP.UnitTest;

public sealed class DkpAwardRequestTests : DatabaseTest
{
    [Fact]
    public async Task Member_can_create_and_cancel_own_request()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Raid attendance", 10, "Attendance", 2));
        var created = await As("member").AwardRequests.CreateAsync(new(preset.Id, 1, "I attended the raid."));

        Assert.Equal(RequestStatus.Pending, created.Status);
        Assert.Equal("I attended the raid.", created.Comment);
        Assert.Single(await new DkpAwardRequestQueries(As("member").Queries).GetMyRequestsAsync());

        await As("member").AwardRequests.CancelAsync(created.Id);

        var cancelled = Assert.Single(await new DkpAwardRequestQueries(As("member").Queries).GetMyRequestsAsync());
        Assert.Equal(RequestStatus.Cancelled, cancelled.Status);
        Assert.Equal(0, await BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Officer_approval_creates_event_with_preset_and_actor()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Boss kill", 25, "Progression", 2));
        var request = await As("member").AwardRequests.CreateAsync(new(preset.Id, 2, null));

        await As("officer").AwardRequests.ApproveAsync(request.Id, new(null));

        var result = Assert.Single(await new DkpAwardRequestQueries(As("officer").Queries).GetAllRequestsAsync());
        Assert.Equal(RequestStatus.Approved, result.Status);
        Assert.Equal(2, result.DkpEventIds.Count);
        Assert.Equal(50, await BalanceAsync(Member.Id));
        await using (var db = Factory.CreateDbContext())
        {
            var state = LedgerReplayState.Replay(await db.DkpEvents.Where(x => x.UserId == Member.Id).ToArrayAsync());
            Assert.Equal(2, state.PresetUsage[preset.Id]);
        }
    }

    [Fact]
    public async Task Duplicate_pending_request_and_member_review_are_rejected()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Once", 10, "Once", 2));
        var request = await As("member").AwardRequests.CreateAsync(new(preset.Id, 1, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => As("member").AwardRequests.CreateAsync(new(preset.Id, 1, null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => As("member").AwardRequests.ApproveAsync(request.Id, new(null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => As("member").AwardRequests.RejectAsync(request.Id, new("No")));
    }

    [Fact]
    public async Task Approval_rechecks_lifetime_limit_and_leaves_request_pending_when_limit_is_reached()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Limited", 10, "Limited", 2));
        var request = await As("member").AwardRequests.CreateAsync(new(preset.Id, 2, null));
        await As("officer").Presets.ApplyAsync(Member.Id, preset.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => As("officer").AwardRequests.ApproveAsync(request.Id, new(null)));

        var unchanged = await new DkpAwardRequestQueries(As("member").Queries).GetRequestAsync(request.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(RequestStatus.Pending, unchanged!.Status);
        Assert.Equal(10, await BalanceAsync(Member.Id));
    }

    [Fact]
    public async Task Rejected_request_does_not_change_balance_or_usage()
    {
        var preset = await As("officer").Presets.CreateAsync(new("Rejected", 10, "Rejected", 2));
        var request = await As("member").AwardRequests.CreateAsync(new(preset.Id, 1, null));

        await As("officer").AwardRequests.RejectAsync(request.Id, new("Not eligible this time."));

        var result = await new DkpAwardRequestQueries(As("member").Queries).GetRequestAsync(request.Id);
        Assert.Equal(RequestStatus.Rejected, result!.Status);
        Assert.Equal("Not eligible this time.", result.ReviewComment);
        Assert.Equal(0, await BalanceAsync(Member.Id));
        await using (var db = Factory.CreateDbContext())
        {
            var state = LedgerReplayState.Replay(await db.DkpEvents.Where(x => x.UserId == Member.Id).ToArrayAsync());
            Assert.Empty(state.PresetUsage);
        }
    }
}
