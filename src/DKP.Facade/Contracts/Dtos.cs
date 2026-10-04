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
public sealed record GuildMemberDto(Guid UserId, string DiscordName, string? AvatarUrl, int DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record CreateDkpTransactionRequest(Guid TargetUserId, int Amount, string Reason);
public sealed record DkpTransactionDto(Guid Id, int Amount, string Reason, DateTime CreatedAtUtc, string CreatedByDiscordName);
public sealed record BalanceDto(int Amount);
public sealed record DkpHistoryDto(BalanceDto Balance, IReadOnlyList<DkpTransactionDto> Transactions);
