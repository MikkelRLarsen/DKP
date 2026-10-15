namespace DKP.Facade.Contracts;

public sealed record BotBulletinItemDto(Guid Id, string Title, string Description, int DkpAmount, int RemainingQuantity, bool UniquePerUser, DateTime? DeadlineUtc);
