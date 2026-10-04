using DKP.Facade.Contracts;

namespace DKP.Facade;

public interface IDkpFacade
{
	Task<DkpHistoryDto?> GetHistoryAsync(string discordId, CancellationToken cancellationToken = default);
}
