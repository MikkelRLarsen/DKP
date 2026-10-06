using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

public sealed class EventLedgerRepository(CommandUnitOfWork session) : IEventLedgerRepository
{
    private DkpDbContext Db => session.Db;
    public async Task<LedgerReplayState> GetStateAsync(Guid? userId = null, CancellationToken ct = default)
    {
        var query = Db.DkpEvents.AsNoTracking();
        if (userId is Guid id)
            query = query.Where(x => x.UserId == id);
        return LedgerReplayState.Replay(await query.ToArrayAsync(ct));
    }
    public async Task<DkpEvent> PostAsync(Guid userId, Guid actorId, Guid operationId, DateTime now, ILedgerPayload payload, CancellationToken ct = default)
    {
        var sequence = (await Db.DkpEvents.Where(x => x.UserId == userId).MaxAsync(x => (long?)x.Sequence, ct) ?? 0) + 1;
        var entry = LedgerEvents.Create(userId, actorId, sequence, operationId, now, payload);
        Db.DkpEvents.Add(entry);
        return entry;
    }
}
