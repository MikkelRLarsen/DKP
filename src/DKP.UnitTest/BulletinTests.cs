using DKP.Domain;
using Xunit;

namespace DKP.UnitTest;

public sealed class BulletinTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Bulletin_item_becomes_unavailable_when_deadline_or_charges_are_reached()
    {
        var item = new BulletinItem("Run", "Complete the run", 25, 2, false, Now.AddHours(1), Now);
        Assert.True(item.IsEffectivelyActive(Now, 0));
        Assert.False(item.IsEffectivelyActive(Now.AddHours(1), 0));
        Assert.False(item.IsEffectivelyActive(Now, 2));
    }

    [Fact]
    public void Claim_reserves_quantity_and_can_only_be_processed_once()
    {
        var userId = Guid.NewGuid();
        var item = new BulletinItem("Run", "Complete the run", 25, 2, true, null, Now);
        var claim = new BulletinClaim(item.Id, userId, 1, "done", Now);
        claim.Approve(Guid.NewGuid(), [Guid.NewGuid()], null, Now);
        Assert.Equal(BulletinClaimStatus.Approved, claim.Status);
        Assert.Throws<InvalidOperationException>(() => claim.Cancel(Now));
    }

    [Fact]
    public void Item_cannot_be_reduced_below_reserved_quantity()
    {
        var item = new BulletinItem("Run", "Complete the run", 25, 2, false, null, Now);
        Assert.Throws<InvalidOperationException>(() => item.Update("Run", "Complete the run", 25, 1, false, null, Now, 2));
    }
}
