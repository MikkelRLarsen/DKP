using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IDiscordForumSectionQueries
{
    Task<IReadOnlyList<DiscordForumSectionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscordForumSectionDto>> GetAllForBotAsync(CancellationToken cancellationToken = default);
}
