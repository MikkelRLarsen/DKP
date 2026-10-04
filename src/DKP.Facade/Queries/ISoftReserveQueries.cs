using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface ISoftReserveQueries
{
	Task<SoftReserveSummaryDto?> GetForUserAsync(
		string authenticatedDiscordId,
		CancellationToken cancellationToken = default);
}
