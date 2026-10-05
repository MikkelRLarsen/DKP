using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IUserBlockCommands
{
	Task<bool> BlockAsync(string officerDiscordId, BlockUserRequest request, CancellationToken cancellationToken = default);
	Task<bool> UnblockAsync(string officerDiscordId, Guid targetUserId, CancellationToken cancellationToken = default);
}
