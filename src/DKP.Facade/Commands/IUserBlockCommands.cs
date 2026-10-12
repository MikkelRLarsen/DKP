using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IUserBlockCommands
{
	Task<bool> BlockAsync(BlockUserRequest request, CancellationToken cancellationToken = default);
	Task<bool> UnblockAsync(Guid targetUserId, CancellationToken cancellationToken = default);
}
