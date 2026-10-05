using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Users;

public sealed class UserBlockCommandService(
	IUserRepository users,
	IOfficerIdentityPolicy officerIdentityPolicy,
	TimeProvider timeProvider) : IUserBlockCommands
{
	public async Task<bool> BlockAsync(string officerDiscordId, BlockUserRequest request, CancellationToken ct = default)
	{
		var officer = await GetOfficerAsync(officerDiscordId, ct);
		var target = await users.FindByIdAsync(request.TargetUserId, ct)
			?? throw new KeyNotFoundException("The selected user does not exist.");

		if (officerIdentityPolicy.IsOfficer(target.DiscordId))
		{
			throw new InvalidOperationException("Configured bootstrap Officers cannot be blocked.");
		}

		var reason = request.Reason?.Trim();
		if (reason?.Length > 500)
		{
			throw new ArgumentException("The block reason cannot exceed 500 characters.", nameof(request));
		}

		target.Block(officer.Id, string.IsNullOrWhiteSpace(reason) ? null : reason, timeProvider.GetUtcNow().UtcDateTime);
		await users.SaveChangesAsync(ct);
		return true;
	}

	public async Task<bool> UnblockAsync(string officerDiscordId, Guid targetUserId, CancellationToken ct = default)
	{
		await GetOfficerAsync(officerDiscordId, ct);
		var target = await users.FindByIdAsync(targetUserId, ct)
			?? throw new KeyNotFoundException("The selected user does not exist.");
		target.Unblock();
		await users.SaveChangesAsync(ct);
		return true;
	}

	private async Task<User> GetOfficerAsync(string discordId, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(discordId))
		{
			throw new UnauthorizedAccessException("An authenticated officer is required.");
		}

		var officer = await users.FindByDiscordIdAsync(discordId, ct)
			?? throw new UnauthorizedAccessException("The authenticated officer does not exist.");
		if (officer.Role != UserRole.Officer || officer.IsBlocked)
		{
			throw new UnauthorizedAccessException("Only active Officers can manage member blocking.");
		}
		return officer;
	}
}
