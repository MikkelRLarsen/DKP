using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpPresetQueries(DkpDbContext db) : IDkpPresetQueries
{
	public async Task<IReadOnlyList<DkpAwardPresetDto>> GetAllAsync(CancellationToken ct = default) => await db.DkpAwardPresets.AsNoTracking().OrderBy(x => x.Name).Select(x => new DkpAwardPresetDto(x.Id,x.Name,x.Amount,x.Reason,x.MaxApplicationsPerUser,x.IsActive)).ToArrayAsync(ct);
	public async Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(string discordId, CancellationToken ct = default)
	{
		var userId = await db.Users.Where(x => x.DiscordId == discordId && !x.IsBlocked).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
		if (userId is null) return [];
		return await db.DkpAwardPresets.AsNoTracking().Where(x => x.IsActive).Select(x => new { x, Applications = db.DkpAwardPresetApplications.Count(a => a.PresetId == x.Id && a.UserId == userId.Value) }).Where(x => x.Applications < x.x.MaxApplicationsPerUser).OrderBy(x => x.x.Name).Select(x => new DkpAcquisitionSourceDto(x.x.Id,x.x.Name,x.x.Amount,x.x.Reason,x.Applications,x.x.MaxApplicationsPerUser,x.x.MaxApplicationsPerUser-x.Applications)).ToArrayAsync(ct);
	}
}
