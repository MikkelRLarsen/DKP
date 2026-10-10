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

    public async Task RequestDeletionAsync(Guid requestId, DateTime nowUtc, CancellationToken ct = default)
    {
        var notification = await session.Db.DiscordNotificationOutbox
            .SingleOrDefaultAsync(x => x.RequestId == requestId && x.NotificationType == "DkpAwardRequestCreated", ct);
        notification?.RequestDeletion(nowUtc);
    }
}
