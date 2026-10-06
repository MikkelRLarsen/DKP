using System.Text.Json;
using System.Text.Json.Serialization;
namespace DKP.Domain;

public interface ILedgerPayload { int Amount { get; } string Reason { get; } }
public enum LootReserveModifierType { SoftReserve, RollBonus }
public sealed record DkpPosted([property: JsonRequired] int Amount, [property: JsonRequired] string Reason, Guid? PresetId = null, Guid? AchievementId = null) : ILedgerPayload;
public sealed record PurchasePlaced([property: JsonRequired] Guid PurchaseId, [property: JsonRequired] Guid ItemId, [property: JsonRequired] string ItemKey, [property: JsonRequired] string ItemName, [property: JsonRequired] int Quantity, [property: JsonRequired] int UnitPrice, [property: JsonRequired] int? RollBonusValue) : ILedgerPayload
{
    public int Amount => checked(-Quantity * UnitPrice);
    public string Reason => $"Purchased {Quantity} x {ItemName}";
}
public sealed record PurchaseCancelled([property: JsonRequired] Guid PurchaseId, [property: JsonRequired] int Amount, [property: JsonRequired] string Reason) : ILedgerPayload;
public sealed record LootReserveConsumed([property: JsonRequired] Guid ConsumeBatchId, [property: JsonRequired] int SoftReserveQuantity, int? RollBonusValue, [property: JsonRequired] IReadOnlyList<Guid> SourcePurchaseIds) : ILedgerPayload
{
    public int Amount => 0;
    public string Reason => "LootReserve consumed";
}
public sealed record LootReserveConsumptionReverted([property: JsonRequired] Guid ConsumeBatchId, [property: JsonRequired] Guid RevertedConsumeEventId) : ILedgerPayload
{
    public int Amount => 0;
    public string Reason => "LootReserve consumption reverted";
}
public sealed record LootReserveModifierGranted([property: JsonRequired] Guid ModifierId, [property: JsonRequired] LootReserveModifierType ModifierType, [property: JsonRequired] int Amount, [property: JsonRequired] int ExportCount, [property: JsonRequired] string GrantReason) : ILedgerPayload
{
    public string Reason => GrantReason;
}
public sealed record LootReserveModifierConsumed([property: JsonRequired] Guid ModifierId, [property: JsonRequired] Guid ConsumeBatchId, [property: JsonRequired] int Amount) : ILedgerPayload
{
    public string Reason => "LootReserve modifier consumed";
}
public sealed record LootReserveModifierRevoked([property: JsonRequired] Guid ModifierId, string? RevokeReason) : ILedgerPayload
{
    public int Amount => 0;
    public string Reason => string.IsNullOrWhiteSpace(RevokeReason) ? "LootReserve modifier revoked" : RevokeReason;
}

/// <summary>The versioned event protocol. All writers and replay use this codec.</summary>
public static class LedgerEvents
{
    public static DkpEvent Create(Guid userId, Guid actorId, long sequence, Guid operationId, DateTime now, ILedgerPayload payload)
    {
        var type = payload switch
        {
            DkpPosted => nameof(DkpPosted),
            PurchasePlaced => nameof(PurchasePlaced),
            PurchaseCancelled => nameof(PurchaseCancelled),
            LootReserveConsumed => nameof(LootReserveConsumed),
            LootReserveConsumptionReverted => nameof(LootReserveConsumptionReverted),
            LootReserveModifierGranted => nameof(LootReserveModifierGranted),
            LootReserveModifierConsumed => nameof(LootReserveModifierConsumed),
            LootReserveModifierRevoked => nameof(LootReserveModifierRevoked),
            _ => throw new InvalidOperationException("Unknown ledger payload.")
        };
        var result = new DkpEvent("UserLedger", userId, sequence, type, userId, actorId, now, operationId,
            JsonSerializer.Serialize(payload, payload.GetType()), 1);
        Read(result);
        return result;
    }

    public static ILedgerPayload Read(DkpEvent entry)
    {
        if (entry.Version != 1 || entry.AggregateType != "UserLedger" || entry.AggregateId != entry.UserId ||
            entry.UserId == Guid.Empty || entry.ActorUserId == Guid.Empty || entry.Sequence <= 0 ||
            entry.CorrelationId == Guid.Empty || entry.OccurredAtUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException($"Invalid event envelope: {entry.Id}.");
        ILedgerPayload payload = entry.EventType switch
        {
            nameof(DkpPosted) => JsonSerializer.Deserialize<DkpPosted>(entry.Payload)!,
            nameof(PurchasePlaced) => JsonSerializer.Deserialize<PurchasePlaced>(entry.Payload)!,
            nameof(PurchaseCancelled) => JsonSerializer.Deserialize<PurchaseCancelled>(entry.Payload)!,
            nameof(LootReserveConsumed) => JsonSerializer.Deserialize<LootReserveConsumed>(entry.Payload)!,
            nameof(LootReserveConsumptionReverted) => JsonSerializer.Deserialize<LootReserveConsumptionReverted>(entry.Payload)!,
            nameof(LootReserveModifierGranted) => JsonSerializer.Deserialize<LootReserveModifierGranted>(entry.Payload)!,
            nameof(LootReserveModifierConsumed) => JsonSerializer.Deserialize<LootReserveModifierConsumed>(entry.Payload)!,
            nameof(LootReserveModifierRevoked) => JsonSerializer.Deserialize<LootReserveModifierRevoked>(entry.Payload)!,
            _ => throw new InvalidOperationException($"Unknown event type: {entry.EventType}.")
        };
        if (payload is null || string.IsNullOrWhiteSpace(payload.Reason) || payload.Reason.Length > 500)
            throw new InvalidOperationException($"Invalid payload: {entry.Id}.");
        if (payload is DkpPosted d && (d.Amount == 0 || d.PresetId == Guid.Empty || (d.PresetId != null && d.Amount < 0)))
            throw new InvalidOperationException("Invalid DKP posting.");
        if (payload is PurchasePlaced p && (p.PurchaseId == Guid.Empty || p.ItemId == Guid.Empty ||
            string.IsNullOrWhiteSpace(p.ItemKey) || p.ItemKey.Length > 64 || string.IsNullOrWhiteSpace(p.ItemName) ||
            p.ItemName.Length > 128 || p.Quantity <= 0 || p.UnitPrice < 0 ||
            (p.RollBonusValue != null && (p.RollBonusValue <= 0 || p.Quantity != 1))))
            throw new InvalidOperationException("Invalid purchase.");
        if (payload is PurchaseCancelled c && (c.PurchaseId == Guid.Empty || c.Amount < 0))
            throw new InvalidOperationException("Invalid cancellation.");
        if (payload is LootReserveConsumed consumed && (consumed.ConsumeBatchId == Guid.Empty || consumed.SoftReserveQuantity < 0 || consumed.SourcePurchaseIds.Any(x => x == Guid.Empty) || consumed.SourcePurchaseIds.Count != consumed.SourcePurchaseIds.Distinct().Count()))
            throw new InvalidOperationException("Invalid LootReserve consumption.");
        if (payload is LootReserveConsumptionReverted reverted && (reverted.ConsumeBatchId == Guid.Empty || reverted.RevertedConsumeEventId == Guid.Empty))
            throw new InvalidOperationException("Invalid LootReserve revert.");
        if (payload is LootReserveModifierGranted granted && (granted.ModifierId == Guid.Empty || granted.Amount <= 0 || granted.ExportCount <= 0 || string.IsNullOrWhiteSpace(granted.GrantReason) || granted.GrantReason.Length > 500))
            throw new InvalidOperationException("Invalid LootReserve modifier grant.");
        if (payload is LootReserveModifierConsumed consumedModifier && (consumedModifier.ModifierId == Guid.Empty || consumedModifier.ConsumeBatchId == Guid.Empty || consumedModifier.Amount <= 0))
            throw new InvalidOperationException("Invalid LootReserve modifier consumption.");
        if (payload is LootReserveModifierRevoked revokedModifier && (revokedModifier.ModifierId == Guid.Empty || revokedModifier.Reason.Length > 500))
            throw new InvalidOperationException("Invalid LootReserve modifier revoke.");
        return payload;
    }
}
