using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotNotificationQueries
{
    Task<IReadOnlyList<DiscordNotificationDto>> ClaimPendingAsync(int limit = 10, CancellationToken cancellationToken = default);
}
