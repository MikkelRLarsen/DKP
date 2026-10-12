using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.LootReserve;

public sealed class LootReserveConsumptionCommandService(CommandContext context, IEventLedgerRepository ledger, TimeProvider time) : ILootReserveConsumptionCommands
{
    public Task<LootReserveConsumptionResultDto> PreviewConsumeAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        => ExecutePreviewAsync(userIds, false, ct);

    public Task<LootReserveConsumptionResultDto> ConsumeAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        => ExecutePreviewAsync(userIds, true, ct);

    public Task<LootReserveConsumptionBatchDto> RevertLatestAsync(CancellationToken ct = default)
        => context.ExecuteAsync([], true, async officer =>
        {
            var events = await ledger.GetAllEventsAsync(ct);
            var reverted = events.Where(x => x.EventType == nameof(LootReserveConsumptionReverted)).Select(x => LedgerEvents.Read(x)).OfType<LootReserveConsumptionReverted>().Select(x => x.RevertedConsumeEventId).ToHashSet();
            var consumptions = events.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id).Where(x => x.EventType == nameof(LootReserveConsumed) && !reverted.Contains(x.Id)).ToArray();
            if (consumptions.Length == 0) throw new InvalidOperationException("There is no LootReserve consumption to revert.");
            var latestBatchId = ((LootReserveConsumed)LedgerEvents.Read(consumptions[0])).ConsumeBatchId;
            var batchEvents = consumptions.Where(x => ((LootReserveConsumed)LedgerEvents.Read(x)).ConsumeBatchId == latestBatchId).ToArray();
            var operationId = Guid.NewGuid();
            foreach (var entry in batchEvents)
                await ledger.PostAsync(entry.UserId, officer.Id, operationId, time.GetUtcNow().UtcDateTime, new LootReserveConsumptionReverted(latestBatchId, entry.Id), ct);
            var payloads = batchEvents.Select(x => (LootReserveConsumed)LedgerEvents.Read(x)).ToArray();
            return new LootReserveConsumptionBatchDto(latestBatchId, time.GetUtcNow().UtcDateTime, officer.DiscordName, batchEvents.Length, payloads.Sum(x => x.SoftReserveQuantity), payloads.Count(x => x.RollBonusValue is not null), false);
        }, ct);

    private Task<LootReserveConsumptionResultDto> ExecutePreviewAsync(IReadOnlyCollection<Guid> rawIds, bool commit, CancellationToken ct)
    {
        var ids = CommandContext.Targets(rawIds);
        return context.ExecuteAsync(ids, true, async officer =>
        {
            var previews = new List<LootReserveConsumptionPreviewDto>();
            var consumeData = new List<(Guid UserId, int SoftQuantity, int? RollBonus, IReadOnlyList<Guid> PurchaseIds, IReadOnlyList<(Guid Id, int Amount)> Modifiers)>();
            foreach (var id in ids)
            {
                var user = await context.TargetAsync(id, ct);
                var state = await ledger.GetStateAsync(id, ct);
                var active = state.Purchases.Values.Where(x => x.CancelledAtUtc is null && !x.IsConsumed).ToArray();
                var soft = active.Where(x => x.ItemKey == "soft-reserve").ToArray();
                var bonuses = active.Where(x => x.RollBonusValue is not null).ToArray();
                var softQuantity = soft.Sum(x => x.Quantity);
                var rollBonus = bonuses.Select(x => x.RollBonusValue).SingleOrDefault();
                var purchaseIds = soft.Select(x => x.PurchaseId).Concat(bonuses.Select(x => x.PurchaseId)).ToArray();
                previews.Add(new LootReserveConsumptionPreviewDto(id, user.DiscordName, softQuantity, rollBonus, true));
                var activeModifiers = state.Modifiers.Values
                    .Where(x => !x.IsRevoked && x.RemainingExports > 0 && (x.ModifierType == LootReserveModifierType.SoftReserve || (x.ModifierType == LootReserveModifierType.RollBonus && rollBonus is not null)))
                    .Select(x => (x.ModifierId, x.Amount))
                    .ToArray();
                if (purchaseIds.Length > 0 || activeModifiers.Length > 0) consumeData.Add((id, softQuantity, rollBonus, purchaseIds, activeModifiers));
            }
            if (!commit)
            {
                var previewBatch = new LootReserveConsumptionBatchDto(Guid.Empty, DateTime.UtcNow, officer.DiscordName, consumeData.Count, consumeData.Sum(x => x.SoftQuantity), consumeData.Count(x => x.RollBonus is not null), false);
                return new LootReserveConsumptionResultDto(previewBatch, previews);
            }
            if (consumeData.Count == 0)
            {
                var emptyBatch = new LootReserveConsumptionBatchDto(Guid.Empty, DateTime.UtcNow, officer.DiscordName, 0, 0, 0, false);
                return new LootReserveConsumptionResultDto(emptyBatch, previews);
            }
            var batchId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var now = time.GetUtcNow().UtcDateTime;
            foreach (var item in consumeData)
            {
                await ledger.PostAsync(item.UserId, officer.Id, operationId, now, new LootReserveConsumed(batchId, item.SoftQuantity, item.RollBonus, item.PurchaseIds), ct);
                foreach (var modifier in item.Modifiers)
                    await ledger.PostAsync(item.UserId, officer.Id, operationId, now, new LootReserveModifierConsumed(modifier.Id, batchId, modifier.Amount), ct);
            }
            var resultBatch = new LootReserveConsumptionBatchDto(batchId, now, officer.DiscordName, consumeData.Count, consumeData.Sum(x => x.SoftQuantity), consumeData.Count(x => x.RollBonus is not null), true);
            return new LootReserveConsumptionResultDto(resultBatch, previews);
        }, ct);
    }
}
