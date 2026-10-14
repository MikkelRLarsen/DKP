using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class BulletinRepository(CommandUnitOfWork unit) : IBulletinRepository
{
    public Task<BulletinItem?> FindItemAsync(Guid id, CancellationToken ct = default) => unit.Db.BulletinItems.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<BulletinClaim?> FindClaimAsync(Guid id, CancellationToken ct = default) => unit.Db.BulletinClaims.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<int> GetReservedQuantityAsync(Guid itemId, CancellationToken ct = default)
        => (await GetClaimsIncludingPendingChangesAsync(itemId, ct))
            .Where(x => x.Status is BulletinClaimStatus.Pending or BulletinClaimStatus.Approved)
            .Sum(x => x.Quantity);

    public async Task<int> GetPendingQuantityAsync(Guid itemId, CancellationToken ct = default)
        => (await GetClaimsIncludingPendingChangesAsync(itemId, ct))
            .Where(x => x.Status == BulletinClaimStatus.Pending)
            .Sum(x => x.Quantity);

    public async Task<bool> HasActiveClaimAsync(Guid itemId, Guid userId, CancellationToken ct = default)
        => (await GetClaimsIncludingPendingChangesAsync(itemId, ct))
            .Any(x => x.UserId == userId && x.Status is BulletinClaimStatus.Pending or BulletinClaimStatus.Approved);
    public Task AddItemAsync(BulletinItem item, CancellationToken ct = default) { unit.Db.BulletinItems.Add(item); return Task.CompletedTask; }
    public Task AddClaimAsync(BulletinClaim claim, CancellationToken ct = default) { unit.Db.BulletinClaims.Add(claim); return Task.CompletedTask; }

    private async Task<IReadOnlyList<BulletinClaim>> GetClaimsIncludingPendingChangesAsync(Guid itemId, CancellationToken ct)
    {
        // SQL filters would evaluate the old persisted status while a command has
        // already changed the tracked entity. Read all rows, then evaluate the
        // current tracked state in memory so outbox payloads are never stale.
        var persisted = await unit.Db.BulletinClaims
            .Where(x => x.BulletinItemId == itemId)
            .ToArrayAsync(ct);
        var byId = persisted.ToDictionary(x => x.Id);
        foreach (var tracked in unit.Db.BulletinClaims.Local.Where(x => x.BulletinItemId == itemId))
            byId[tracked.Id] = tracked;
        return byId.Values.ToArray();
    }
}
