using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

/// <summary>Read-only member data requested by the Discord bot.</summary>
public interface IBotDkpQueries
{
    Task<DkpHistoryDto?> GetHistoryAsync(string discordId, int? limit = null, CancellationToken cancellationToken = default);
}
