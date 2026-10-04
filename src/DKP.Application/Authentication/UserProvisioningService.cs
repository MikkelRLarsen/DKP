using DKP.Application.Persistence;
using DKP.Domain;

namespace DKP.Application.Authentication;

public sealed class UserProvisioningService(
	IUserRepository users,
	IOfficerIdentityPolicy officerIdentityPolicy,
	TimeProvider timeProvider) : IUserProvisioningService
{
	public async Task<User> ProvisionAsync(DiscordUserProfile profile, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(profile.DiscordId))
		{
			throw new ArgumentException("Discord user ID is required.", nameof(profile));
		}

		if (string.IsNullOrWhiteSpace(profile.DiscordName))
		{
			throw new ArgumentException("Discord display name is required.", nameof(profile));
		}

		var role = officerIdentityPolicy.IsOfficer(profile.DiscordId)
			? UserRole.Officer
			: UserRole.Member;
		var user = await users.FindByDiscordIdAsync(profile.DiscordId, cancellationToken);

		if (user is null)
		{
			user = new User(profile.DiscordId, profile.DiscordName, profile.AvatarUrl, role, timeProvider.GetUtcNow().UtcDateTime);
			await users.AddAsync(user, cancellationToken);
		}
		else
		{
			user.UpdateProfile(profile.DiscordName, profile.AvatarUrl, role);
		}

		await users.SaveChangesAsync(cancellationToken);
		return user;
	}
}
