namespace DKP.Facade.Contracts;

public sealed record DkpAwardPresetDto(Guid Id, string Name, int Amount, string Reason, int MaxApplicationsPerUser, bool IsActive);
public sealed record DkpAwardPresetInput(string Name, int Amount, string Reason, int MaxApplicationsPerUser);
public sealed record DkpPresetUsageDto(Guid PresetId, int Applications, int MaxApplications, int Remaining)
{
	public bool IsAvailable => Remaining > 0;
}
public sealed record DkpAcquisitionSourceDto(Guid PresetId, string Name, int Amount, string Reason, int Applications, int MaxApplications, int Remaining);
