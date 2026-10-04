using DKP.Facade.Contracts;
using DKP.Facade.Queries;

namespace DKP.Facade;

public sealed class DkpFacade(IDkpQueries queries) : IDkpFacade
{
	public Task<DkpHistoryDto?> GetHistoryAsync(string discordId, CancellationToken cancellationToken = default)
		=> queries.GetHistoryAsync(discordId, cancellationToken);
}
