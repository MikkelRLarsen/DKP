namespace DKP.Facade.Contracts;
public sealed record AchievementDefinitionDto(Guid Id, string Key, string Name, string Description, int DkpAmount, bool IsActive);
public sealed record AchievementInput(string Key, string Name, string Description, int DkpAmount);
public sealed record UserAchievementDto(Guid Id, Guid UserId, string DiscordName, Guid AchievementId, string AchievementName, int DkpAmount, bool IsActive, DateTime GrantedAtUtc, DateTime? RevokedAtUtc);
