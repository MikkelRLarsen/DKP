namespace DKP.Facade.Contracts;

public sealed record BotAccountDto(Guid Id, string DiscordId, string DiscordName, UserRole Role, bool Created);
public sealed record BotAccountInput(string DiscordName, string? AvatarUrl);
