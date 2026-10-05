namespace DKP.Domain;
public sealed class DkpBalanceProjection
{
	private DkpBalanceProjection() { }
	public DkpBalanceProjection(Guid userId) { UserId = userId; }
	public Guid UserId { get; private set; }
	public int Balance { get; private set; }
	public Guid? LastEventId { get; private set; }
	public DateTime UpdatedAtUtc { get; private set; }
	public void Apply(int amount, Guid eventId, DateTime now) { Balance = checked(Balance + amount); LastEventId = eventId; UpdatedAtUtc = now; }
}
