using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpPresetQueries(DkpDbContext db) : IDkpPresetQueries
{
	public async Task<IReadOnlyList<DkpAwardPresetDto>> GetAllAsync(CancellationToken ct = default) => await db.DkpAwardPresets.AsNoTracking().OrderBy(x => x.Name).Select(x => new DkpAwardPresetDto(x.Id,x.Name,x.Amount,x.Reason,x.MaxApplicationsPerUser,x.IsActive)).ToArrayAsync(ct);
	public async Task<DkpPresetUsageDto> GetUsageAsync(string officerDiscordId, Guid targetUserId, Guid presetId, CancellationToken ct = default)
	{
		var officer = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == officerDiscordId, ct);
		if (officer is null || officer.Role != DKP.Domain.UserRole.Officer || officer.IsBlocked)
			throw new UnauthorizedAccessException("Only active Officers can view preset usage.");

		var preset = await db.DkpAwardPresets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == presetId, ct)
			?? throw new KeyNotFoundException("Preset not found.");
		if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == targetUserId, ct))
			throw new KeyNotFoundException("Target user not found.");

		var applications = await db.DkpAwardPresetApplications.AsNoTracking()
			.CountAsync(x => x.PresetId == presetId && x.UserId == targetUserId, ct);
		return new DkpPresetUsageDto(presetId, applications, preset.MaxApplicationsPerUser, Math.Max(0, preset.MaxApplicationsPerUser - applications));
	}
	public async Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(string discordId, CancellationToken ct = default)
	{
		var userId = await db.Users.Where(x => x.DiscordId == discordId && !x.IsBlocked).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
		if (userId is null) return [];
		return await db.DkpAwardPresets.AsNoTracking().Where(x => x.IsActive).Select(x => new { x, Applications = db.DkpAwardPresetApplications.Count(a => a.PresetId == x.Id && a.UserId == userId.Value) }).Where(x => x.Applications < x.x.MaxApplicationsPerUser).OrderBy(x => x.x.Name).Select(x => new DkpAcquisitionSourceDto(x.x.Id,x.x.Name,x.x.Amount,x.x.Reason,x.Applications,x.x.MaxApplicationsPerUser,x.x.MaxApplicationsPerUser-x.Applications)).ToArrayAsync(ct);
	}
}
