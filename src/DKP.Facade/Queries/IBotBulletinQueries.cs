using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotBulletinQueries
{
    Task<IReadOnlyList<BotBulletinItemDto>?> GetAvailableItemsAsync(string discordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BulletinClaimDto>?> GetPendingClaimsAsync(string discordId, CancellationToken cancellationToken = default);
}
