namespace DKP.Facade.Contracts;

public sealed record DiscordNotificationDto(Guid Id, string NotificationType, string Payload, int Attempts, string Action, ulong? DiscordMessageId, string? RecipientDiscordUserId);
