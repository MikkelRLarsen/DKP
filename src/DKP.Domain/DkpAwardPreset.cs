namespace DKP.Domain;

public sealed class DkpAwardPreset
{
	private DkpAwardPreset() { }

	public DkpAwardPreset(string name, int amount, string reason, int maxApplicationsPerUser, DateTime now)
	{
		Id = Guid.NewGuid(); Name = name; Amount = amount; Reason = reason;
		MaxApplicationsPerUser = maxApplicationsPerUser; IsActive = true; CreatedAtUtc = now; UpdatedAtUtc = now;
	}

	public Guid Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public int Amount { get; private set; }
	public string Reason { get; private set; } = string.Empty;
	public int MaxApplicationsPerUser { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DateTime UpdatedAtUtc { get; private set; }

	public void Update(string name, int amount, string reason, int max, DateTime now)
	{ Name = name; Amount = amount; Reason = reason; MaxApplicationsPerUser = max; UpdatedAtUtc = now; }
	public void SetActive(bool active, DateTime now) { IsActive = active; UpdatedAtUtc = now; }
}
