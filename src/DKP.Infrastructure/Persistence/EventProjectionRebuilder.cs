using DKP.Application.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

public sealed class EventProjectionRebuilder(CommandUnitOfWork session) : IEventProjectionRebuilder
{
    public Task RebuildAsync(CancellationToken cancellationToken = default)
        => session.ExecuteAsync([], async () =>
        {
            var db = session.Db;
            await db.LedgerEntries.ExecuteDeleteAsync(cancellationToken);
            await db.DkpAwardPresetApplications.ExecuteDeleteAsync(cancellationToken);
            await db.ShopPurchaseProjections.ExecuteDeleteAsync(cancellationToken);
            await db.DkpBalanceProjections.ExecuteDeleteAsync(cancellationToken);
            var events = await db.DkpEvents.AsNoTracking()
                .OrderBy(x => x.AggregateType).ThenBy(x => x.AggregateId).ThenBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
            foreach (var entry in events)
                await LedgerProjector.ApplyAsync(db, entry, cancellationToken);
            return true;
        }, cancellationToken);
}
