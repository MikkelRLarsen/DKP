using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class AchievementQueries(QuerySession session) : IAchievementQueries
{
    public Task<IReadOnlyList<AchievementDefinitionDto>> GetDefinitionsAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<AchievementDefinitionDto>>(true, async (db, _) => await db.AchievementDefinitions.AsNoTracking().OrderBy(x => x.Name).Select(x => new AchievementDefinitionDto(x.Id, x.Key, x.Name, x.Description, x.DkpAmount, x.IsActive)).ToArrayAsync(ct), ct);
    public Task<IReadOnlyList<UserAchievementDto>> GetUserAchievementsAsync(Guid? userId = null, CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<UserAchievementDto>>(userId is null, async (db, actor) =>
    {
        if (userId is not null && userId != actor.Id && actor.Role != DKP.Domain.UserRole.Officer) throw new UnauthorizedAccessException("You cannot view another user's achievements.");
        var query = db.UserAchievements.AsNoTracking().AsQueryable();
        if (userId is Guid targetId) query = query.Where(x => x.UserId == targetId);
        else if (actor.Role != DKP.Domain.UserRole.Officer) query = query.Where(x => x.UserId == actor.Id);
        var rows = await query.OrderByDescending(x => x.GrantedAtUtc).ToArrayAsync(ct);
        var users = await db.Users.AsNoTracking().Where(x => rows.Select(r => r.UserId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var definitions = await db.AchievementDefinitions.AsNoTracking().Where(x => rows.Select(r => r.AchievementId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return rows.Where(x => definitions.ContainsKey(x.AchievementId) && users.ContainsKey(x.UserId)).Select(x => { var definition = definitions[x.AchievementId]; return new UserAchievementDto(x.Id, x.UserId, users[x.UserId].DiscordName, x.AchievementId, definition.Name, definition.DkpAmount, x.RevokedAtUtc is null, x.GrantedAtUtc, x.RevokedAtUtc); }).ToArray();
    }, ct);
}
