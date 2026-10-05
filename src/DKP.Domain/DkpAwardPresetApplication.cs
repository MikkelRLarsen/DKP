namespace DKP.Domain;

public sealed class DkpAwardPresetApplication
{
	private DkpAwardPresetApplication() { }

	public DkpAwardPresetApplication(Guid presetId, Guid userId, Guid? dkpTransactionId, Guid appliedByUserId, DateTime now)
	{ Id = Guid.NewGuid(); PresetId = presetId; UserId = userId; DkpTransactionId = dkpTransactionId; AppliedByUserId = appliedByUserId; CreatedAtUtc = now; }

	public Guid Id { get; private set; }
	public Guid PresetId { get; private set; }
	public Guid UserId { get; private set; }
	public Guid? DkpTransactionId { get; private set; }
	public Guid AppliedByUserId { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public DkpAwardPreset Preset { get; private set; } = null!;
	public User User { get; private set; } = null!;
	public DkpTransaction? DkpTransaction { get; private set; }
	public User AppliedByUser { get; private set; } = null!;
}
