using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IAchievementCommands
{
    Task<AchievementDefinitionDto> CreateAsync(AchievementInput input, CancellationToken cancellationToken = default);
    Task<AchievementDefinitionDto> UpdateAsync(Guid id, AchievementInput input, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAchievementDto>> GrantAsync(Guid achievementId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid userAchievementId, CancellationToken cancellationToken = default);
}
