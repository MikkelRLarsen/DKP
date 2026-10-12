using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotAchievementQueries
{
    Task<BotAchievementOverviewDto?> GetOverviewAsync(string discordId, CancellationToken cancellationToken = default);
}
