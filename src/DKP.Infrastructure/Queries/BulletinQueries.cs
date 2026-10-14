using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BulletinQueries(QuerySession session, TimeProvider time) : IBulletinQueries
{
    public Task<IReadOnlyList<BulletinItemDto>> GetAvailableItemsAsync(CancellationToken ct = default) => session.ReadAsync(false, (db, actor) => LoadItemsAsync(db, true, actor.Id, ct), ct);
    public Task<IReadOnlyList<BulletinClaimDto>> GetMyClaimsAsync(CancellationToken ct = default) => session.ReadAsync(false, (db, actor) => LoadClaimsAsync(db, actor.Id, ct), ct);
    public Task<IReadOnlyList<BulletinItemDto>> GetAllItemsAsync(CancellationToken ct = default) => session.ReadAsync(true, (db, _) => LoadItemsAsync(db, false, null, ct), ct);
    public Task<IReadOnlyList<BulletinClaimDto>> GetAllClaimsAsync(CancellationToken ct = default) => session.ReadAsync(true, (db, _) => LoadClaimsAsync(db, null, ct), ct);
    private async Task<IReadOnlyList<BulletinItemDto>> LoadItemsAsync(DkpDbContext db, bool availableOnly, Guid? actorId, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var items = await db.BulletinItems.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var claims = await db.BulletinClaims.AsNoTracking().Where(x => ids.Contains(x.BulletinItemId) && (x.Status == DKP.Domain.BulletinClaimStatus.Pending || x.Status == DKP.Domain.BulletinClaimStatus.Approved)).ToArrayAsync(ct);
        return items.Select(item =>
        {
            var pending = claims.Where(x => x.BulletinItemId == item.Id && x.Status == DKP.Domain.BulletinClaimStatus.Pending).Sum(x => x.Quantity);
            var approved = claims.Where(x => x.BulletinItemId == item.Id && x.Status == DKP.Domain.BulletinClaimStatus.Approved).Sum(x => x.Quantity);
            var reserved = pending + approved;
            var hasActiveClaim = actorId is Guid userId && item.UniquePerUser && claims.Any(x => x.BulletinItemId == item.Id && x.UserId == userId && (x.Status == DKP.Domain.BulletinClaimStatus.Pending || x.Status == DKP.Domain.BulletinClaimStatus.Approved));
            return new BulletinItemDto(item.Id, item.Title, item.Description, item.DkpAmount, item.MaxCompletions, pending, approved, Math.Max(0, item.MaxCompletions - reserved), item.UniquePerUser, item.DeadlineUtc, item.IsActive, item.IsEffectivelyActive(now, reserved), item.CreatedAtUtc, item.UpdatedAtUtc, item.IsHidden, hasActiveClaim);
        }).Where(x => !availableOnly || (x.IsEffectivelyActive && !x.IsHidden)).OrderBy(x => x.HasActiveClaim ? 1 : 0).ThenByDescending(x => x.CreatedAtUtc).ToArray();
    }
    private static async Task<IReadOnlyList<BulletinClaimDto>> LoadClaimsAsync(DkpDbContext db, Guid? userId, CancellationToken ct)
    {
        var query = db.BulletinClaims.AsNoTracking().AsQueryable();
        if (userId is Guid id) query = query.Where(x => x.UserId == id);
        var claims = await query.OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
        if (claims.Length == 0) return [];
        var userIds = claims.SelectMany(x => new[] { x.UserId, x.ReviewedByUserId ?? Guid.Empty }).Where(x => x != Guid.Empty).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var itemIds = claims.Select(x => x.BulletinItemId).Distinct().ToArray();
        var items = await db.BulletinItems.AsNoTracking().Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return claims.Select(claim =>
        {
            var user = users[claim.UserId]; var item = items[claim.BulletinItemId]; var main = user.Characters.FirstOrDefault(x => x.IsMain);
            var reviewer = claim.ReviewedByUserId is Guid id && users.TryGetValue(id, out var reviewerUser) ? reviewerUser.DiscordName : null;
            return new BulletinClaimDto(claim.Id, item.Id, user.Id, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", item.Title, claim.Quantity, claim.Comment, (DKP.Facade.Contracts.BulletinClaimStatus)claim.Status, claim.CreatedAtUtc, claim.ReviewedAtUtc, reviewer, claim.ReviewComment, claim.DkpEventIds);
        }).ToArray();
    }
}
