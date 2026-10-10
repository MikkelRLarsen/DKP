using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotNotificationQueries(IDbContextFactory<Persistence.DkpDbContext> factory) : IBotNotificationQueries
{
    public async Task<IReadOnlyList<DiscordNotificationDto>> ClaimPendingAsync(int limit = 10, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var until = now.AddMinutes(2);
        var rows = await db.DiscordNotificationOutbox.Where(x => x.NextAttemptAtUtc <= now && (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc < now) &&
                ((x.SentAtUtc == null && x.DeleteRequestedAtUtc == null) || (x.SentAtUtc != null && x.DeleteRequestedAtUtc != null && x.DeletedAtUtc == null && x.DiscordMessageId != null)))
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Take(limit).ToArrayAsync(ct);
        foreach (var row in rows) row.Claim(now, until);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return rows.Select(x => new DiscordNotificationDto(x.Id, x.NotificationType, x.Payload, x.Attempts,
            x.SentAtUtc is null ? "send" : "delete", x.DiscordMessageId)).ToArray();
    }
}
