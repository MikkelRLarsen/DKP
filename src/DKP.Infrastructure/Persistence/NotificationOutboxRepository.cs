using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class NotificationOutboxRepository(CommandUnitOfWork session) : INotificationOutboxRepository
{
    public Task AddAsync(DiscordNotificationOutbox notification, CancellationToken ct = default)
    {
        session.Db.DiscordNotificationOutbox.Add(notification);
        return Task.CompletedTask;
    }

    public async Task SuppressPendingReviewNotificationsAsync(string recipientDiscordId, DateTime nowUtc, CancellationToken ct = default)
    {
        var notifications = await session.Db.DiscordNotificationOutbox
            .Where(x => x.NotificationType == "DkpAwardRequestReviewed" &&
                        x.RecipientDiscordUserId == recipientDiscordId &&
                        x.SentAtUtc == null &&
                        x.DeleteRequestedAtUtc == null)
            .ToArrayAsync(ct);
        foreach (var notification in notifications)
            notification.RequestDeletion(nowUtc);
    }

    public async Task RequestDeletionAsync(Guid requestId, DateTime nowUtc, CancellationToken ct = default, string notificationType = "DkpAwardRequestCreated")
    {
        var notification = await session.Db.DiscordNotificationOutbox
            .SingleOrDefaultAsync(x => x.RequestId == requestId && x.NotificationType == notificationType, ct);
        notification?.RequestDeletion(nowUtc);
    }

    public async Task SyncBulletinAsync(Guid bulletinItemId, string payload, bool delete, DateTime nowUtc, CancellationToken ct = default)
    {
        var notification = await session.Db.DiscordNotificationOutbox
            .SingleOrDefaultAsync(x => x.RequestId == bulletinItemId && x.NotificationType == "BulletinItem", ct);

        if (notification is null)
        {
            if (!delete)
                session.Db.DiscordNotificationOutbox.Add(new DiscordNotificationOutbox("BulletinItem", payload, nowUtc, bulletinItemId));
            return;
        }

        if (delete)
        {
            notification.RequestDeletion(nowUtc);
            return;
        }

        notification.QueueUpdate(payload, nowUtc);
    }

    public async Task SyncSourceAsync(Guid sourceId, string sourceName, string payload, bool delete, DateTime nowUtc, CancellationToken ct = default)
    {
        var notifications = await session.Db.DiscordNotificationOutbox
            .Where(x => x.NotificationType == "DkpSourceCreated" && (x.RequestId == sourceId || x.RequestId == null))
            .ToArrayAsync(ct);
        var notification = notifications.FirstOrDefault(x => x.RequestId == sourceId);
        if (notification is null)
        {
            notification = notifications.FirstOrDefault(x =>
            {
                try
                {
                    using var document = System.Text.Json.JsonDocument.Parse(x.Payload);
                    var root = document.RootElement;
                    return root.TryGetProperty("Name", out var name) && name.GetString() == sourceName;
                }
                catch (System.Text.Json.JsonException) { return false; }
            });
        }

        if (notification is null)
        {
            if (!delete)
                session.Db.DiscordNotificationOutbox.Add(new DiscordNotificationOutbox("DkpSourceCreated", payload, nowUtc, sourceId));
            return;
        }

        if (delete)
        {
            notification.RequestDeletion(nowUtc);
            return;
        }

        notification.QueueUpdate(payload, nowUtc);
    }

    public async Task SyncShopItemAsync(Guid shopItemId, string itemName, string payload, bool delete, DateTime nowUtc, CancellationToken ct = default)
    {
        var notification = await session.Db.DiscordNotificationOutbox
            .SingleOrDefaultAsync(x => x.RequestId == shopItemId && x.NotificationType == "ShopItem", ct);

        if (notification is null)
        {
            if (!delete)
                session.Db.DiscordNotificationOutbox.Add(new DiscordNotificationOutbox("ShopItem", payload, nowUtc, shopItemId));
            return;
        }

        if (delete)
        {
            notification.RequestDeletion(nowUtc);
            return;
        }

        notification.QueueUpdate(payload, nowUtc);
    }
}
