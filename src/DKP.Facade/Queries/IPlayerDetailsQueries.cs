using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IPlayerDetailsQueries
{
	Task<PlayerDetailsDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
