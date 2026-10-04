using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IDkpPresetQueries
{
	Task<IReadOnlyList<DkpAwardPresetDto>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(string authenticatedDiscordId, CancellationToken cancellationToken = default);
}
