using DKP.Domain;

namespace DKP.Application.Authentication;

public interface IUserProvisioningService
{
	Task<User> ProvisionAsync(DiscordUserProfile profile, CancellationToken cancellationToken = default);
	Task<UserProvisioningResult> ProvisionWithStatusAsync(DiscordUserProfile profile, CancellationToken cancellationToken = default);
}

public sealed record UserProvisioningResult(User User, bool Created);
