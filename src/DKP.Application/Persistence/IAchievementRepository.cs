using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IAchievementRepository
{
    Task<AchievementDefinition?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AchievementDefinition achievement, CancellationToken cancellationToken = default);
    Task<UserAchievement?> FindUserAchievementAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default);
    Task AddUserAchievementAsync(UserAchievement achievement, CancellationToken cancellationToken = default);
}
