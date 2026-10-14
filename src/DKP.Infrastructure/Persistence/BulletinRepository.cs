using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class BulletinRepository(CommandUnitOfWork unit) : IBulletinRepository
{
    public Task<BulletinItem?> FindItemAsync(Guid id, CancellationToken ct = default) => unit.Db.BulletinItems.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<BulletinClaim?> FindClaimAsync(Guid id, CancellationToken ct = default) => unit.Db.BulletinClaims.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<int> GetReservedQuantityAsync(Guid itemId, CancellationToken ct = default) => await unit.Db.BulletinClaims.Where(x => x.BulletinItemId == itemId && (x.Status == BulletinClaimStatus.Pending || x.Status == BulletinClaimStatus.Approved)).Select(x => (int?)x.Quantity).SumAsync(ct) ?? 0;
    public async Task<int> GetPendingQuantityAsync(Guid itemId, CancellationToken ct = default) => await unit.Db.BulletinClaims.Where(x => x.BulletinItemId == itemId && x.Status == BulletinClaimStatus.Pending).Select(x => (int?)x.Quantity).SumAsync(ct) ?? 0;
    public Task<bool> HasActiveClaimAsync(Guid itemId, Guid userId, CancellationToken ct = default) => unit.Db.BulletinClaims.AnyAsync(x => x.BulletinItemId == itemId && x.UserId == userId && (x.Status == BulletinClaimStatus.Pending || x.Status == BulletinClaimStatus.Approved), ct);
    public Task AddItemAsync(BulletinItem item, CancellationToken ct = default) { unit.Db.BulletinItems.Add(item); return Task.CompletedTask; }
    public Task AddClaimAsync(BulletinClaim claim, CancellationToken ct = default) { unit.Db.BulletinClaims.Add(claim); return Task.CompletedTask; }
}
