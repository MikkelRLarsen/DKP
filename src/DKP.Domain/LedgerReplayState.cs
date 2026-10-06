namespace DKP.Domain;

public sealed class LedgerReplayState
{
    private readonly Dictionary<Guid, LedgerPurchaseState> purchases = [];
    private readonly Dictionary<Guid, int> presetUsage = [];
    private readonly List<LedgerActivityState> activities = [];
    private readonly Dictionary<Guid, LedgerConsumptionState> consumptions = [];
    private readonly HashSet<Guid> revertedConsumptions = [];

    public int Balance { get; private set; }
    public IReadOnlyDictionary<Guid, LedgerPurchaseState> Purchases => purchases;
    public IReadOnlyDictionary<Guid, int> PresetUsage => presetUsage;
    public IReadOnlyList<LedgerActivityState> Activities => activities;
    public IReadOnlyDictionary<Guid, LedgerConsumptionState> Consumptions => consumptions;

    public static LedgerReplayState Replay(IEnumerable<DkpEvent> events)
    {
        var ordered = events.OrderBy(x => x.AggregateType).ThenBy(x => x.AggregateId).ThenBy(x => x.Sequence).ThenBy(x => x.Id).ToArray();
        var state = new LedgerReplayState();
        var lastSequence = new Dictionary<Guid, long>();

        foreach (var entry in ordered)
        {
            var payload = LedgerEvents.Read(entry);
            var previous = lastSequence.GetValueOrDefault(entry.AggregateId);
            if (entry.Sequence != previous + 1)
                throw new InvalidOperationException($"Broken event sequence for {entry.AggregateId}.");
            lastSequence[entry.AggregateId] = entry.Sequence;

            switch (payload)
            {
                case DkpPosted posted:
                    state.Balance = checked(state.Balance + posted.Amount);
                    if (posted.PresetId is Guid presetId)
                        state.presetUsage[presetId] = state.presetUsage.GetValueOrDefault(presetId) + 1;
                    state.activities.Add(LedgerActivityState.From(entry, posted));
                    break;

                case PurchasePlaced purchase:
                    if (state.purchases.ContainsKey(purchase.PurchaseId))
                        throw new InvalidOperationException($"Duplicate purchase {purchase.PurchaseId}.");
                    state.Balance = checked(state.Balance + purchase.Amount);
                    state.purchases.Add(purchase.PurchaseId, new LedgerPurchaseState(entry, purchase));
                    state.activities.Add(LedgerActivityState.From(entry, purchase));
                    break;

                case PurchaseCancelled cancellation:
                    if (!state.purchases.TryGetValue(cancellation.PurchaseId, out var existing))
                        throw new InvalidOperationException($"Cancellation without purchase {cancellation.PurchaseId}.");
                    if (existing.CancelledAtUtc is not null || existing.TotalDkpCost != cancellation.Amount)
                        throw new InvalidOperationException($"Purchase {cancellation.PurchaseId} was already cancelled or has a mismatched refund.");
                    existing.Cancel(entry);
                    state.Balance = checked(state.Balance + cancellation.Amount);
                    state.activities.Add(LedgerActivityState.From(entry, cancellation, existing));
                    break;

                case LootReserveConsumed consumed:
                    if (state.consumptions.Values.Any(x => x.BatchId == consumed.ConsumeBatchId))
                        throw new InvalidOperationException($"Duplicate consume batch {consumed.ConsumeBatchId}.");
                    var sourcePurchases = consumed.SourcePurchaseIds.Select(id =>
                        state.purchases.TryGetValue(id, out var purchase) ? purchase : throw new InvalidOperationException($"Consumption references unknown purchase {id}."))
                        .ToArray();
                    if (sourcePurchases.Any(x => x.CancelledAtUtc is not null || x.IsConsumed))
                        throw new InvalidOperationException("Consumption references an inactive purchase.");
                    if (sourcePurchases.Any(x => x.ItemKey != "soft-reserve" && x.RollBonusValue is null))
                        throw new InvalidOperationException("Consumption references a non-LootReserve purchase.");
                    var softQuantity = sourcePurchases.Where(x => x.ItemKey == "soft-reserve").Sum(x => x.Quantity);
                    var bonusPurchases = sourcePurchases.Where(x => x.RollBonusValue is not null).ToArray();
                    if (bonusPurchases.Length > 1)
                        throw new InvalidOperationException("Consumption references multiple RollBonus purchases.");
                    var rollBonus = bonusPurchases.Select(x => x.RollBonusValue).SingleOrDefault();
                    if (softQuantity != consumed.SoftReserveQuantity || rollBonus != consumed.RollBonusValue)
                        throw new InvalidOperationException("Consumption payload does not match its purchases.");
                    if (softQuantity == 0 && rollBonus is null)
                        throw new InvalidOperationException("Consumption contains no active LootReserve state.");
                    foreach (var purchase in sourcePurchases) purchase.Consume();
                    state.consumptions.Add(entry.Id, new LedgerConsumptionState(entry.Id, consumed.ConsumeBatchId, entry.UserId, consumed.SourcePurchaseIds, entry.OccurredAtUtc));
                    break;

                case LootReserveConsumptionReverted reverted:
                    if (!state.consumptions.TryGetValue(reverted.RevertedConsumeEventId, out var consumption) || consumption.BatchId != reverted.ConsumeBatchId)
                        throw new InvalidOperationException("Revert references an unknown consumption.");
                    if (!state.revertedConsumptions.Add(reverted.RevertedConsumeEventId))
                        throw new InvalidOperationException("Consumption was already reverted.");
                    foreach (var purchaseId in consumption.SourcePurchaseIds)
                        state.purchases[purchaseId].Restore();
                    break;
            }
        }

        return state;
    }

    public int ActiveQuantity(Guid itemId) => purchases.Values.Where(x => x.ShopItemId == itemId && x.CancelledAtUtc is null).Sum(x => x.ActiveQuantity);
    public bool HasActiveRollBonus() => purchases.Values.Any(x => x.RollBonusValue is not null && x.CancelledAtUtc is null && !x.IsConsumed);
}

public sealed class LedgerPurchaseState
{
    public LedgerPurchaseState(DkpEvent entry, PurchasePlaced purchase)
    {
        PurchaseId = purchase.PurchaseId;
        UserId = entry.UserId;
        ShopItemId = purchase.ItemId;
        ItemKey = purchase.ItemKey;
        ItemName = purchase.ItemName;
        Quantity = purchase.Quantity;
        UnitPrice = purchase.UnitPrice;
        TotalDkpCost = checked(purchase.Quantity * purchase.UnitPrice);
        RollBonusValue = purchase.RollBonusValue;
        CreatedByUserId = entry.ActorUserId;
        CreatedAtUtc = entry.OccurredAtUtc;
        PurchaseEventId = entry.Id;
    }

    public Guid PurchaseId { get; }
    public Guid UserId { get; }
    public Guid ShopItemId { get; }
    public string ItemKey { get; }
    public string ItemName { get; }
    public int Quantity { get; }
    public int UnitPrice { get; }
    public int TotalDkpCost { get; }
    public int? RollBonusValue { get; }
    public Guid CreatedByUserId { get; }
    public DateTime CreatedAtUtc { get; }
    public Guid PurchaseEventId { get; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? CancellationEventId { get; private set; }
    public bool IsConsumed { get; private set; }
    public int ActiveQuantity => IsConsumed ? 0 : Quantity;

    public void Cancel(DkpEvent entry)
    {
        CancelledAtUtc = entry.OccurredAtUtc;
        CancellationEventId = entry.Id;
    }

    public void Consume() => IsConsumed = true;
    public void Restore() => IsConsumed = false;
}

public sealed record LedgerConsumptionState(Guid EventId, Guid BatchId, Guid UserId, IReadOnlyList<Guid> SourcePurchaseIds, DateTime CreatedAtUtc);

public sealed record LedgerActivityState(Guid EventId, Guid UserId, Guid ActorUserId, string Action, int Amount, string Reason, string? ItemName, int? Quantity, DateTime CreatedAtUtc)
{
    public static LedgerActivityState From(DkpEvent entry, DkpPosted payload) => new(entry.Id, entry.UserId, entry.ActorUserId, payload.PresetId is null ? "manual" : "preset", payload.Amount, payload.Reason, null, null, entry.OccurredAtUtc);
    public static LedgerActivityState From(DkpEvent entry, PurchasePlaced payload) => new(entry.Id, entry.UserId, entry.ActorUserId, "purchase", payload.Amount, payload.Reason, payload.ItemName, payload.Quantity, entry.OccurredAtUtc);
    public static LedgerActivityState From(DkpEvent entry, PurchaseCancelled payload, LedgerPurchaseState purchase) => new(entry.Id, entry.UserId, entry.ActorUserId, "cancellation", payload.Amount, payload.Reason, purchase.ItemName, purchase.Quantity, entry.OccurredAtUtc);
}
