using DKP.Domain;

namespace DKP.Application.Authentication;

public interface IUserProvisioningService
{
	Task<User> ProvisionAsync(DiscordUserProfile profile, CancellationToken cancellationToken = default);
}
