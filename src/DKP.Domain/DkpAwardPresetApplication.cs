namespace DKP.Domain;
/// <summary>Rebuildable lifetime usage; its identity is the actual ledger event.</summary>
public sealed class DkpAwardPresetApplication
{
    private DkpAwardPresetApplication() { }
    public DkpAwardPresetApplication(Guid presetId, DkpEvent entry)
    {
        DkpEventId = entry.Id; PresetId = presetId; UserId = entry.UserId;
        AppliedByUserId = entry.ActorUserId; CreatedAtUtc = entry.OccurredAtUtc;
    }
    public Guid DkpEventId { get; private set; }
    public Guid PresetId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AppliedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
