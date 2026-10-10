namespace DKP.Facade.Commands;

public interface IBotNotificationCommands
{
    Task MarkSentAsync(Guid notificationId, ulong messageId, CancellationToken cancellationToken = default);
    Task MarkDeletedAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid notificationId, string? error, CancellationToken cancellationToken = default);
}
