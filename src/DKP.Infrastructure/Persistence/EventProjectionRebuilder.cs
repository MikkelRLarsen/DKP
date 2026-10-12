using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

/// <summary>Validates and replays the append-only event stream without database writes.</summary>
public sealed class EventProjectionRebuilder(IDbContextFactory<DkpDbContext> factory) : IEventProjectionRebuilder
{
    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var events = await db.DkpEvents.AsNoTracking().ToArrayAsync(cancellationToken);
        _ = LedgerReplayState.Replay(events);
    }
}
