using DKP.Domain;

namespace DKP.Application.Persistence;

public interface INotificationOutboxRepository
{
    Task AddAsync(DiscordNotificationOutbox notification, CancellationToken cancellationToken = default);
    Task SuppressPendingReviewNotificationsAsync(string recipientDiscordId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task RequestDeletionAsync(Guid requestId, DateTime nowUtc, CancellationToken cancellationToken = default, string notificationType = "DkpAwardRequestCreated");
    Task SyncBulletinAsync(Guid bulletinItemId, string payload, bool delete, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task SyncSourceAsync(Guid sourceId, string sourceName, string payload, bool delete, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task SyncShopItemAsync(Guid shopItemId, string itemName, string payload, bool delete, DateTime nowUtc, CancellationToken cancellationToken = default);
}
