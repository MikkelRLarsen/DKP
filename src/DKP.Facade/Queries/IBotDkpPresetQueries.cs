using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotDkpPresetQueries
{
    Task<IReadOnlyList<DkpAcquisitionSourceDto>?> GetAvailableSourcesAsync(string discordId, CancellationToken cancellationToken = default);
}
