using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IAchievementQueries
{
    Task<IReadOnlyList<AchievementDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAchievementDto>> GetUserAchievementsAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}
