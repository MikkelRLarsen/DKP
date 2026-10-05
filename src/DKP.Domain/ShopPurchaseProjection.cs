namespace DKP.Domain;
public sealed class ShopPurchaseProjection
{
    private ShopPurchaseProjection() { }
    public ShopPurchaseProjection(DkpEvent entry, PurchasePlaced purchase)
    {
        PurchaseId = purchase.PurchaseId; UserId = entry.UserId; ShopItemId = purchase.ItemId;
        ItemKey = purchase.ItemKey; ItemName = purchase.ItemName; Quantity = purchase.Quantity;
        UnitPrice = purchase.UnitPrice; TotalDkpCost = checked(purchase.Quantity * purchase.UnitPrice);
        RollBonusValue = purchase.RollBonusValue; CreatedByUserId = entry.ActorUserId;
        CreatedAtUtc = entry.OccurredAtUtc; PurchaseEventId = entry.Id;
    }
    public Guid PurchaseId { get; private set; }
    public Guid PurchaseEventId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ShopItemId { get; private set; }
    public string ItemKey { get; private set; } = "";
    public string ItemName { get; private set; } = "";
    public int Quantity { get; private set; }
    public int UnitPrice { get; private set; }
    public int TotalDkpCost { get; private set; }
    public int? RollBonusValue { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? CancellationEventId { get; private set; }
    public void Cancel(DkpEvent entry)
    {
        if (CancelledAtUtc != null) throw new InvalidOperationException("Purchase already cancelled.");
        CancelledAtUtc = entry.OccurredAtUtc;
        CancellationEventId = entry.Id;
    }
}
