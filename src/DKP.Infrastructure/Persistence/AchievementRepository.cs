using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class AchievementRepository(CommandUnitOfWork unit) : IAchievementRepository
{
    public Task<AchievementDefinition?> FindAsync(Guid id, CancellationToken ct = default) => unit.Db.AchievementDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(AchievementDefinition achievement, CancellationToken ct = default) { unit.Db.AchievementDefinitions.Add(achievement); return Task.CompletedTask; }
    public Task<UserAchievement?> FindUserAchievementAsync(Guid id, CancellationToken ct = default) => unit.Db.UserAchievements.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> HasActiveAsync(Guid userId, Guid achievementId, CancellationToken ct = default) => unit.Db.UserAchievements.AnyAsync(x => x.UserId == userId && x.AchievementId == achievementId && x.RevokedAtUtc == null, ct);
    public Task AddUserAchievementAsync(UserAchievement achievement, CancellationToken ct = default) { unit.Db.UserAchievements.Add(achievement); return Task.CompletedTask; }
}
