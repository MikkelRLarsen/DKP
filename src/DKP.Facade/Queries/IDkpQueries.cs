using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IDkpQueries
{
	Task<DkpHistoryDto?> GetHistoryAsync(CancellationToken cancellationToken = default);
	Task<DkpHistoryDto?> GetPlayerHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);
}
