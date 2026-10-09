using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotDkpQueries(IDbContextFactory<DkpDbContext> factory) : IBotDkpQueries
{
    public async Task<DkpHistoryDto?> GetHistoryAsync(string discordId, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return null;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DiscordId == discordId, cancellationToken);
        if (user is null || user.IsBlocked) return null;

        var history = await ReadModels.HistoryAsync(db, user.Id, cancellationToken);
        if (limit is not > 0) return history;

        return history with
        {
            Transactions = history.Transactions.Take(Math.Min(limit.Value, 50)).ToArray()
        };
    }
}
