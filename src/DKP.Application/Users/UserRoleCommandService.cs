using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Users;

public sealed class UserRoleCommandService(
	IUserRepository users,
	IOfficerIdentityPolicy officerIdentityPolicy) : IUserRoleCommands
{
	public async Task<bool> SetRoleAsync(
		string officerDiscordId,
		SetUserRoleRequest request,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(officerDiscordId))
		{
			throw new UnauthorizedAccessException("An authenticated officer is required.");
		}

		var officer = await users.FindByDiscordIdAsync(officerDiscordId, cancellationToken)
			?? throw new UnauthorizedAccessException("The authenticated officer does not exist.");

		if (officer.Role != UserRole.Officer)
		{
			throw new UnauthorizedAccessException("Only Officers can manage user roles.");
		}

		if (!Enum.IsDefined(request.Role))
		{
			throw new ArgumentException("The requested role is invalid.", nameof(request.Role));
		}

		var target = await users.FindByIdAsync(request.TargetUserId, cancellationToken)
			?? throw new KeyNotFoundException("The selected user does not exist.");

		if (request.Role == UserRole.Member && officerIdentityPolicy.IsOfficer(target.DiscordId))
		{
			throw new InvalidOperationException("Configured bootstrap Officers cannot be demoted.");
		}

		target.SetRole(request.Role);
		await users.SaveChangesAsync(cancellationToken);
		return true;
	}
}
