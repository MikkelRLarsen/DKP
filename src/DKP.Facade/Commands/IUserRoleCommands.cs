using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IUserRoleCommands
{
	Task<bool> SetRoleAsync(
		string officerDiscordId,
		SetUserRoleRequest request,
		CancellationToken cancellationToken = default);
}
