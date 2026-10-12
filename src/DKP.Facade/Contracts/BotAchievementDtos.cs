namespace DKP.Facade.Contracts;

public sealed record BotAchievementOverviewDto(
    IReadOnlyList<AchievementDefinitionDto> Definitions,
    IReadOnlyList<UserAchievementDto> UserAchievements,
    IReadOnlyList<DkpAwardRequestDto> Requests);

