using DKP.Facade.Commands;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class BotNotificationCommands(IDbContextFactory<DkpDbContext> factory) : IBotNotificationCommands
{
    public async Task MarkSentAsync(Guid id, ulong messageId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.DiscordNotificationOutbox.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Notification not found.");
        row.MarkSent(DateTime.UtcNow, messageId);
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkDeletedAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.DiscordNotificationOutbox.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Notification not found.");
        row.MarkDeleted(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string? error, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.DiscordNotificationOutbox.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Notification not found.");
        row.MarkFailed(DateTime.UtcNow, string.IsNullOrWhiteSpace(error) ? "Discord delivery failed." : error);
        await db.SaveChangesAsync(ct);
    }
}
