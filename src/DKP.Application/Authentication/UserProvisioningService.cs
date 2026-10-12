using DKP.Application.Persistence;
using DKP.Domain;

namespace DKP.Application.Authentication;

public sealed class UserProvisioningService(
	IUserRepository users,
	IOfficerIdentityPolicy officerIdentityPolicy,
	TimeProvider timeProvider,
	ICommandUnitOfWork unitOfWork) : IUserProvisioningService
{
	public Task<User> ProvisionAsync(DiscordUserProfile profile, CancellationToken cancellationToken = default)
		=> unitOfWork.ExecuteAsync([], async () =>
	{
		if (string.IsNullOrWhiteSpace(profile.DiscordId))
		{
			throw new ArgumentException("Discord user ID is required.", nameof(profile));
		}

		if (string.IsNullOrWhiteSpace(profile.DiscordName))
		{
			throw new ArgumentException("Discord display name is required.", nameof(profile));
		}

		var user = await users.FindByDiscordIdAsync(profile.DiscordId, cancellationToken);

		if (user is null)
		{
			var role = officerIdentityPolicy.IsOfficer(profile.DiscordId)
				? UserRole.Officer
				: UserRole.Member;
			user = new User(profile.DiscordId, profile.DiscordName, profile.AvatarUrl, role, timeProvider.GetUtcNow().UtcDateTime);
			await users.AddAsync(user, cancellationToken);
		}
		else
		{
			if (user.IsBlocked) throw new UnauthorizedAccessException("This user is blocked.");
			var role = officerIdentityPolicy.IsOfficer(profile.DiscordId) ? UserRole.Officer : user.Role;
			user.UpdateProfile(profile.DiscordName, profile.AvatarUrl, role);
		}
		return user;
	}, cancellationToken);
}
