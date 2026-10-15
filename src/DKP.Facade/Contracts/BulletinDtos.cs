namespace DKP.Facade.Contracts;

public enum BulletinClaimStatus { Pending, Approved, Rejected, Cancelled, Reverted }
public sealed record CreateBulletinItemRequest(string Title, string Description, int DkpAmount, int MaxCompletions, bool UniquePerUser, DateTime? DeadlineUtc, Guid? TemplateId = null);
public sealed record UpdateBulletinItemRequest(string Title, string Description, int DkpAmount, int MaxCompletions, bool UniquePerUser, DateTime? DeadlineUtc);
public sealed record BulletinTemplateDto(Guid Id, string Name, string Title, string Description, int DkpAmount, int MaxCompletions, bool UniquePerUser, TimeSpan? DefaultDeadlineOffset, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateBulletinTemplateRequest(string Name, string Title, string Description, int DkpAmount, int MaxCompletions, bool UniquePerUser, TimeSpan? DefaultDeadlineOffset);
public sealed record UpdateBulletinTemplateRequest(string Name, string Title, string Description, int DkpAmount, int MaxCompletions, bool UniquePerUser, TimeSpan? DefaultDeadlineOffset);
public sealed record CreateBulletinClaimRequest(Guid BulletinItemId, int Quantity, string? Comment);
public sealed record ReviewBulletinClaimRequest(string? Comment);
public sealed record BulletinItemDto(Guid Id, string Title, string Description, int DkpAmount, int MaxCompletions, int PendingQuantity, int ApprovedQuantity, int RemainingQuantity, bool UniquePerUser, DateTime? DeadlineUtc, bool IsActive, bool IsEffectivelyActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, bool IsHidden, bool HasActiveClaim);
public sealed record BulletinClaimDto(Guid Id, Guid BulletinItemId, Guid UserId, string DiscordName, string? MainCharacter, string BulletinTitle, int Quantity, string? Comment, BulletinClaimStatus Status, DateTime CreatedAtUtc, DateTime? ReviewedAtUtc, string? ReviewerName, string? ReviewComment, IReadOnlyList<Guid> DkpEventIds);
