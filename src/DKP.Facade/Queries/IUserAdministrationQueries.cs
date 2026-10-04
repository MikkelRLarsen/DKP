using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IUserAdministrationQueries
{
	Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken = default);
}
