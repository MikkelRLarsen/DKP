namespace DKP.Domain;

public sealed class DkpTransaction
{
	private DkpTransaction()
	{
	}

	public DkpTransaction(Guid userId, int amount, string reason, Guid createdByUserId, DateTime createdAtUtc)
	{
		Id = Guid.NewGuid();
		UserId = userId;
		Amount = amount;
		Reason = reason;
		CreatedByUserId = createdByUserId;
		CreatedAtUtc = createdAtUtc;
	}

	public Guid Id { get; private set; }
	public Guid UserId { get; private set; }
	public int Amount { get; private set; }
	public string Reason { get; private set; } = string.Empty;
	public Guid CreatedByUserId { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public User User { get; private set; } = null!;
	public User CreatedByUser { get; private set; } = null!;
}
