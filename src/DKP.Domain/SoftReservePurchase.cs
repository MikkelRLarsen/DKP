namespace DKP.Domain;

public sealed class SoftReservePurchase
{
	private SoftReservePurchase()
	{
	}

	public SoftReservePurchase(Guid userId, int quantity, int dkpCost, DateTime createdAtUtc)
	{
		Id = Guid.NewGuid();
		UserId = userId;
		Quantity = quantity;
		DkpCost = dkpCost;
		CreatedAtUtc = createdAtUtc;
	}

	public Guid Id { get; private set; }
	public Guid UserId { get; private set; }
	public int Quantity { get; private set; }
	public int DkpCost { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime? CancelledAtUtc { get; private set; }
	public User User { get; private set; } = null!;

	public bool IsCancelled => CancelledAtUtc is not null;

	public void Cancel(DateTime cancelledAtUtc)
	{
		if (IsCancelled)
		{
			throw new InvalidOperationException("The Soft Reserve purchase has already been cancelled.");
		}

		CancelledAtUtc = cancelledAtUtc;
	}
}
