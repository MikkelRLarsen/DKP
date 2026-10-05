namespace DKP.Domain;
/// <summary>One display/history row per event, including purchases and refunds.</summary>
public sealed class LedgerEntryProjection
{
    private LedgerEntryProjection() { }
    public LedgerEntryProjection(DkpEvent entry, ILedgerPayload payload, string? itemName = null, int? quantity = null)
    {
        EventId = entry.Id; UserId = entry.UserId; ActorUserId = entry.ActorUserId;
        CreatedAtUtc = entry.OccurredAtUtc; Sequence = entry.Sequence;
        Amount = payload.Amount; Reason = payload.Reason; ItemName = itemName; Quantity = quantity;
        Action = payload switch { DkpPosted { PresetId: not null } => "preset", DkpPosted => "manual", PurchasePlaced => "purchase", PurchaseCancelled => "cancellation", _ => throw new InvalidOperationException() };
    }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public long Sequence { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string Action { get; private set; } = "";
    public int Amount { get; private set; }
    public string Reason { get; private set; } = "";
    public string? ItemName { get; private set; }
    public int? Quantity { get; private set; }
}
