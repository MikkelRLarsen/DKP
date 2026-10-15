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
}
