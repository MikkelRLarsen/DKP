using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class LootReserveConsumptionQueries(QuerySession session) : ILootReserveConsumptionQueries
{
    public Task<LootReserveConsumptionBatchDto?> GetLatestConsumptionBatchAsync(CancellationToken ct = default)
        => session.ReadAsync(true, async (db, _) =>
        {
            var events = await db.DkpEvents.AsNoTracking().ToArrayAsync(ct);
            var reverted = events.Where(x => x.EventType == nameof(LootReserveConsumptionReverted)).Select(x => LedgerEvents.Read(x)).OfType<LootReserveConsumptionReverted>().Select(x => x.RevertedConsumeEventId).ToHashSet();
            var candidates = events.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id).Where(x => x.EventType == nameof(LootReserveConsumed) && !reverted.Contains(x.Id)).ToArray();
            if (candidates.Length == 0) return null;
            var latest = (LootReserveConsumed)LedgerEvents.Read(candidates[0]);
            var batch = candidates.Where(x => ((LootReserveConsumed)LedgerEvents.Read(x)).ConsumeBatchId == latest.ConsumeBatchId).ToArray();
            var actorId = batch[0].ActorUserId;
            var actorName = await db.Users.AsNoTracking().Where(x => x.Id == actorId).Select(x => x.DiscordName).SingleOrDefaultAsync(ct) ?? "Unknown";
            var payloads = batch.Select(x => (LootReserveConsumed)LedgerEvents.Read(x)).ToArray();
            return new LootReserveConsumptionBatchDto(latest.ConsumeBatchId, batch.Max(x => x.OccurredAtUtc), actorName, batch.Length, payloads.Sum(x => x.SoftReserveQuantity), payloads.Count(x => x.RollBonusValue is not null), true);
        }, ct);
}
