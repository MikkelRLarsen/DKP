using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IAccountQueries
{
	Task<DashboardDto?> GetDashboardAsync(string discordId, CancellationToken cancellationToken = default);
}
