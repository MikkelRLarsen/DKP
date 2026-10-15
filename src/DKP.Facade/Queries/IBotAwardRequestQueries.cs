using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotAwardRequestQueries
{
    Task<IReadOnlyList<DkpAwardRequestDto>?> GetPendingAsync(string discordId, CancellationToken cancellationToken = default);
}
