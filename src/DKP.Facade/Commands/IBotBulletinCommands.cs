using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotBulletinCommands
{
    Task<BulletinClaimDto> ClaimAsync(string discordId, Guid bulletinItemId, int quantity, CancellationToken cancellationToken = default);
    Task ApproveAsync(string discordId, Guid claimId, CancellationToken cancellationToken = default);
    Task RejectAsync(string discordId, Guid claimId, CancellationToken cancellationToken = default);
}
