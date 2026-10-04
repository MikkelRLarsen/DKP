using DKP.Domain;

namespace DKP.Facade.Contracts;

public sealed record CharacterDto(Guid Id, string FirstName, string LastName);
public sealed record DashboardDto(Guid UserId, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role, decimal DkpBalance, IReadOnlyList<CharacterDto> Characters);
public sealed record CharacterInput(string FirstName, string LastName);
public sealed record UserSummary(Guid Id, string DiscordId, string DiscordName, string? AvatarUrl, UserRole Role);
