using System.Text.Json;
namespace DKP.Domain;

public interface ILedgerPayload { int Amount { get; } string Reason { get; } }
public sealed record DkpPosted(int Amount, string Reason, Guid? PresetId = null) : ILedgerPayload;
public sealed record PurchasePlaced(Guid PurchaseId, Guid ItemId, string ItemKey, string ItemName, int Quantity, int UnitPrice, int? RollBonusValue) : ILedgerPayload
{
    public int Amount => checked(-Quantity * UnitPrice);
    public string Reason => $"Purchased {Quantity} x {ItemName}";
}
public sealed record PurchaseCancelled(Guid PurchaseId, int Amount, string Reason) : ILedgerPayload;

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
        return payload;
    }
}
