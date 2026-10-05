namespace DKP.Domain;
public sealed class ShopPurchaseProjection
{
	private ShopPurchaseProjection() { }
	public ShopPurchaseProjection(Guid purchaseId, Guid userId, Guid itemId, int quantity, int totalCost, Guid actorUserId, DateTime createdAtUtc) { PurchaseId = purchaseId; UserId = userId; ShopItemId = itemId; Quantity = quantity; TotalDkpCost = totalCost; CreatedByUserId = actorUserId; CreatedAtUtc = createdAtUtc; }
	public Guid PurchaseId { get; private set; }
	public Guid UserId { get; private set; }
	public Guid ShopItemId { get; private set; }
	public int Quantity { get; private set; }
	public int TotalDkpCost { get; private set; }
	public Guid CreatedByUserId { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime? CancelledAtUtc { get; private set; }
	public void Cancel(DateTime now) { if (CancelledAtUtc is not null) throw new InvalidOperationException("Purchase has already been cancelled."); CancelledAtUtc = now; }
}
