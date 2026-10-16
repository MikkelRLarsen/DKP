using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotNotificationPreferenceCommands
{
    Task<BotReviewNotificationPreferenceDto> ToggleReviewNotificationsAsync(string discordId, CancellationToken cancellationToken = default);
}
