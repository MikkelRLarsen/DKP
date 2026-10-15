using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotBulletinQueries(IDbContextFactory<Persistence.DkpDbContext> factory, TimeProvider time) : IBotBulletinQueries
{
    public async Task<IReadOnlyList<BotBulletinItemDto>?> GetAvailableItemsAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        var now = time.GetUtcNow().UtcDateTime;
        var items = await db.BulletinItems.AsNoTracking()
            .Where(x => x.IsActive && !x.IsHidden && (!x.DeadlineUtc.HasValue || x.DeadlineUtc > now))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var claims = await db.BulletinClaims.AsNoTracking()
            .Where(x => ids.Contains(x.BulletinItemId) && (x.Status == DKP.Domain.BulletinClaimStatus.Pending || x.Status == DKP.Domain.BulletinClaimStatus.Approved))
            .ToArrayAsync(ct);
        return items.Select(item =>
        {
            var reserved = claims.Where(x => x.BulletinItemId == item.Id).Sum(x => x.Quantity);
            return new BotBulletinItemDto(item.Id, item.Title, item.Description, item.DkpAmount, Math.Max(0, item.MaxCompletions - reserved), item.UniquePerUser, item.DeadlineUtc);
        }).Where(x => x.RemainingQuantity > 0).ToArray();
    }

    public async Task<IReadOnlyList<BulletinClaimDto>?> GetPendingClaimsAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var officer = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (officer is null || officer.IsBlocked || officer.Role != DKP.Domain.UserRole.Officer) return null;

        var claims = await db.BulletinClaims.AsNoTracking()
            .Where(x => x.Status == DKP.Domain.BulletinClaimStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(ct);
        if (claims.Length == 0) return [];

        var userIds = claims.Select(x => x.UserId).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var itemIds = claims.Select(x => x.BulletinItemId).Distinct().ToArray();
        var items = await db.BulletinItems.AsNoTracking().Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return claims.Select(claim =>
        {
            var user = users[claim.UserId];
            var item = items[claim.BulletinItemId];
            var main = user.Characters.FirstOrDefault(x => x.IsMain);
            return new BulletinClaimDto(claim.Id, item.Id, user.Id, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", item.Title, claim.Quantity, claim.Comment, DKP.Facade.Contracts.BulletinClaimStatus.Pending, claim.CreatedAtUtc, null, null, null, claim.DkpEventIds);
        }).ToArray();
    }
}
