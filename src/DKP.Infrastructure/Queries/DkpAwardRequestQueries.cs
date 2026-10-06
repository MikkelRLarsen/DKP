using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class DkpAwardRequestQueries(QuerySession session) : IDkpAwardRequestQueries
{
    public Task<IReadOnlyList<DkpAwardRequestDto>> GetMyRequestsAsync(CancellationToken ct = default) => session.ReadAsync(false, (db, actor) => LoadAsync(db, actor.Id, ct), ct);
    public Task<IReadOnlyList<DkpAwardRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default) => session.ReadAsync(true, (db, _) => LoadAsync(db, null, ct, true), ct);
    public Task<IReadOnlyList<DkpAwardRequestDto>> GetAllRequestsAsync(CancellationToken ct = default) => session.ReadAsync(true, (db, _) => LoadAsync(db, null, ct), ct);
    public Task<DkpAwardRequestDto?> GetRequestAsync(Guid id, CancellationToken ct = default) => session.ReadAsync(false, async (db, actor) =>
    {
        var request = await db.DkpAwardRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (request is null) return null;
        if (request.UserId != actor.Id && actor.Role != DKP.Domain.UserRole.Officer) throw new UnauthorizedAccessException("You cannot view this request.");
        return (await LoadAsync(db, request.UserId, ct)).SingleOrDefault(x => x.Id == id);
    }, ct);

    private static async Task<IReadOnlyList<DkpAwardRequestDto>> LoadAsync(DkpDbContext db, Guid? userId, CancellationToken ct, bool pendingOnly = false)
    {
        var query = db.DkpAwardRequests.AsNoTracking().AsQueryable();
        if (userId is Guid id) query = query.Where(x => x.UserId == id);
        if (pendingOnly) query = query.Where(x => x.Status == DKP.Domain.DkpAwardRequestStatus.Pending);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(ct);
        if (rows.Length == 0) return [];
        var userIds = rows.SelectMany(x => new[] { x.UserId, x.ReviewedByUserId ?? Guid.Empty }).Where(x => x != Guid.Empty).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var presetIds = rows.Where(x => x.PresetId is not null).Select(x => x.PresetId!.Value).Distinct().ToArray();
        var presets = await db.DkpAwardPresets.AsNoTracking().Where(x => presetIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var achievementIds = rows.Where(x => x.AchievementId is not null).Select(x => x.AchievementId!.Value).Distinct().ToArray();
        var achievements = await db.AchievementDefinitions.AsNoTracking().Where(x => achievementIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return rows.Select(row =>
        {
            var user = users[row.UserId];
            var preset = row.PresetId is Guid presetId ? presets[presetId] : null;
            var achievement = row.AchievementId is Guid achievementId ? achievements[achievementId] : null;
            var reviewer = row.ReviewedByUserId is Guid reviewerId && users.TryGetValue(reviewerId, out var reviewerUser) ? reviewerUser.DiscordName : null;
            var main = user.Characters.FirstOrDefault(x => x.IsMain);
            return new DkpAwardRequestDto(row.Id, row.UserId, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", row.PresetId, row.AchievementId, achievement?.Name ?? preset!.Name, achievement?.DkpAmount ?? preset!.Amount, row.Quantity, achievement?.Description ?? preset!.Reason, row.Comment, ToStatus(row.Status), row.CreatedAtUtc, row.ReviewedAtUtc, reviewer, row.ReviewComment, row.DkpEventIds);
        }).ToArray();
    }

    private static DKP.Facade.Contracts.DkpAwardRequestStatus ToStatus(DKP.Domain.DkpAwardRequestStatus status) => status switch
    {
        DKP.Domain.DkpAwardRequestStatus.Pending => DKP.Facade.Contracts.DkpAwardRequestStatus.Pending,
        DKP.Domain.DkpAwardRequestStatus.Approved => DKP.Facade.Contracts.DkpAwardRequestStatus.Approved,
        DKP.Domain.DkpAwardRequestStatus.Rejected => DKP.Facade.Contracts.DkpAwardRequestStatus.Rejected,
        DKP.Domain.DkpAwardRequestStatus.Cancelled => DKP.Facade.Contracts.DkpAwardRequestStatus.Cancelled,
        _ => throw new InvalidOperationException("Unknown request status.")
    };
}
