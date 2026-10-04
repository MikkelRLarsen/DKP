using DKP.Domain;

namespace DKP.Facade.Contracts;

public sealed record CharacterDto(Guid Id, string FirstName, string LastName, bool IsMain);
public sealed record DashboardDto(Guid UserId, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role, int DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record CharacterInput(string FirstName, string LastName);
public sealed record UserSummary(Guid Id, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role, string? MainCharacterName)
{
	public string DisplayName => MainCharacterName is null
		? DiscordName
		: $"{DiscordName} / {MainCharacterName}";
}
public sealed record SetUserRoleRequest(Guid TargetUserId, UserRole Role);
public sealed record GuildMemberDto(Guid UserId, string DiscordName, string? AvatarUrl, int DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record PlayerDetailsDto(Guid UserId, string DiscordName, string? AvatarUrl, IReadOnlyList<CharacterDto> Characters, DkpHistoryDto DkpHistory);
public sealed record CreateDkpTransactionRequest(Guid TargetUserId, int Amount, string Reason);
public sealed record DkpTransactionDto(Guid Id, int Amount, string Reason, DateTime CreatedAtUtc, string CreatedByDiscordName);
public sealed record BalanceDto(int Amount);
public sealed record DkpHistoryDto(BalanceDto Balance, IReadOnlyList<DkpTransactionDto> Transactions);
public sealed record PurchaseSoftReserveRequest(int Quantity);
public sealed record SoftReservePurchaseDto(Guid Id, int Quantity, int DkpCost, DateTime CreatedAtUtc, DateTime? CancelledAtUtc)
{
	public bool IsCancelled => CancelledAtUtc is not null;
}
public sealed record DkpPurchaseOptionDto(string Key, string Name, string Description, int UnitCost);
public sealed record SoftReserveSummaryDto(int DkpCost, int MaxReserves, int ActiveQuantity, IReadOnlyList<DkpPurchaseOptionDto> Options, IReadOnlyList<SoftReservePurchaseDto> Purchases);
