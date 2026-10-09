using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotAchievementRequestCommands
{
    Task<DkpAwardRequestDto> CreateAsync(string discordId, Guid achievementId, string? comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DkpAwardRequestDto>> CreateForUsersAsync(string discordId, BotMultiAchievementRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(string discordId, Guid requestId, CancellationToken cancellationToken = default);
}
