namespace DKP.Domain;

public sealed class ShopPurchase
{
	private ShopPurchase() { }

	public ShopPurchase(Guid userId, Guid shopItemId, int quantity, int totalDkpCost, Guid createdByUserId, DateTime createdAtUtc)
	{
		Id = Guid.NewGuid(); UserId = userId; ShopItemId = shopItemId; Quantity = quantity;
		TotalDkpCost = totalDkpCost; CreatedByUserId = createdByUserId; CreatedAtUtc = createdAtUtc;
	}

	public Guid Id { get; private set; }
	public Guid UserId { get; private set; }
	public Guid ShopItemId { get; private set; }
	public int Quantity { get; private set; }
	public int TotalDkpCost { get; private set; }
	public Guid CreatedByUserId { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime? CancelledAtUtc { get; private set; }
	public User User { get; private set; } = null!;
	public ShopItem ShopItem { get; private set; } = null!;
	public User CreatedByUser { get; private set; } = null!;
	public bool IsCancelled => CancelledAtUtc is not null;

	public void Cancel(DateTime now)
	{
		if (IsCancelled) throw new InvalidOperationException("The purchase has already been cancelled.");
		CancelledAtUtc = now;
	}
}
