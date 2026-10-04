using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IDkpQueries
{
	Task<DkpHistoryDto?> GetHistoryAsync(string discordId, CancellationToken cancellationToken = default);
}
