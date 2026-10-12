using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IAccountQueries
{
	Task<DashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default);
}
