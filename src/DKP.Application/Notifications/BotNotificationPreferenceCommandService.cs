using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Notifications;

public sealed class BotNotificationPreferenceCommandService(
    IUserRepository users,
    INotificationOutboxRepository outbox,
    ICommandUnitOfWork unitOfWork,
    TimeProvider time) : IBotNotificationPreferenceCommands
{
    public Task<BotReviewNotificationPreferenceDto> ToggleReviewNotificationsAsync(string discordId, CancellationToken cancellationToken = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            if (string.IsNullOrWhiteSpace(discordId))
                throw new UnauthorizedAccessException("An active member is required.");

            var user = await users.FindByDiscordIdAsync(discordId, cancellationToken);
            if (user is null || user.IsBlocked)
                throw new UnauthorizedAccessException("An active member is required.");

            var enabled = !user.ReviewNotificationsEnabled;
            user.SetReviewNotificationsEnabled(enabled);
            // Discard anything waiting in the outbox on both transitions. This
            // prevents notifications created while disabled from being sent
            // later when notifications are enabled again.
            await outbox.SuppressPendingReviewNotificationsAsync(discordId, time.GetUtcNow().UtcDateTime, cancellationToken);
            return new BotReviewNotificationPreferenceDto(enabled);
        }, cancellationToken);
}
