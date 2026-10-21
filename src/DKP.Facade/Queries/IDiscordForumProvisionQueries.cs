using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IDiscordForumProvisionQueries
{
    Task<IReadOnlyList<DiscordForumProvisionDto>> GetPendingForBotAsync(CancellationToken cancellationToken = default);
    Task<DiscordForumProvisionDto?> GetLatestAsync(string guildId, string sectionKey, CancellationToken cancellationToken = default);
}
