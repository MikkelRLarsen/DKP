namespace DKP.Facade.Contracts;

public sealed record BotMultiAchievementRequest(Guid AchievementId, IReadOnlyList<string> TargetDiscordIds, string? Comment);
