namespace DKP.Facade.Contracts;

public sealed record CharacterDto(Guid Id, string FirstName, string LastName, bool IsMain);
public sealed record DashboardDto(Guid UserId, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role, int DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record CharacterInput(string FirstName, string LastName);
public sealed record UserSummary(Guid Id, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role, string? MainCharacterName, bool IsBlocked = false, DateTime? BlockedAtUtc = null, string? BlockReason = null)
{
	public string DisplayName => MainCharacterName is null
		? DiscordName
		: $"{DiscordName} / {MainCharacterName}";
}
public sealed record SetUserRoleRequest(Guid TargetUserId, UserRole Role);
public sealed record BlockUserRequest(Guid TargetUserId, string? Reason);
public sealed record GuildMemberDto(Guid UserId, string DiscordName, string? AvatarUrl, int DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record PlayerDetailsDto(Guid UserId, string DiscordName, string? AvatarUrl, IReadOnlyList<CharacterDto> Characters, DkpHistoryDto DkpHistory);
