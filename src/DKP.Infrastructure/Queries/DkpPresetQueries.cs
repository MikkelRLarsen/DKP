using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpPresetQueries(QuerySession session) : IDkpPresetQueries
{
    public Task<IReadOnlyList<DkpAwardPresetDto>> GetAllAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<DkpAwardPresetDto>>(true, async (db, _) =>
            await db.DkpAwardPresets.OrderBy(p => p.Name).ThenBy(p => p.Id)
                .Select(p => new DkpAwardPresetDto(p.Id, p.Name, p.Amount, p.Reason, p.MaxApplicationsPerUser, p.IsActive)).ToArrayAsync(ct), ct);
    public Task<DkpPresetUsageDto> GetUsageAsync(Guid targetUserId, Guid presetId, CancellationToken ct = default)
        => session.ReadAsync(true, async (db, _) =>
        {
            var preset = await db.DkpAwardPresets.SingleOrDefaultAsync(p => p.Id == presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!await db.Users.AnyAsync(u => u.Id == targetUserId, ct)) throw new KeyNotFoundException("Player not found.");
            var count = await db.DkpAwardPresetApplications.CountAsync(a => a.PresetId == presetId && a.UserId == targetUserId, ct);
            return new DkpPresetUsageDto(presetId, count, preset.MaxApplicationsPerUser, Math.Max(0, preset.MaxApplicationsPerUser - count));
        }, ct);
    public Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<DkpAcquisitionSourceDto>>(false, async (db, actor) =>
        {
            var rows = await db.DkpAwardPresets.Where(p => p.IsActive).OrderBy(p => p.Name).Select(p => new
            { Preset = p, Count = db.DkpAwardPresetApplications.Count(a => a.PresetId == p.Id && a.UserId == actor.Id) }).ToListAsync(ct);
            return rows.Where(x => x.Count < x.Preset.MaxApplicationsPerUser).Select(x => new DkpAcquisitionSourceDto(
                x.Preset.Id, x.Preset.Name, x.Preset.Amount, x.Preset.Reason, x.Count, x.Preset.MaxApplicationsPerUser, x.Preset.MaxApplicationsPerUser - x.Count)).ToArray();
        }, ct);
}
