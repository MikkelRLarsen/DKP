using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpPresetQueries(QuerySession session) : IDkpPresetQueries
{
    public Task<IReadOnlyList<DkpAwardPresetDto>> GetAllAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<DkpAwardPresetDto>>(true, async (db, _) => await db.DkpAwardPresets.OrderBy(p => p.Name).ThenBy(p => p.Id).Select(p => new DkpAwardPresetDto(p.Id, p.Name, p.Amount, p.Reason, p.MaxApplicationsPerUser, p.IsActive)).ToArrayAsync(ct), ct);
    public Task<DkpPresetUsageDto> GetUsageAsync(Guid targetUserId, Guid presetId, CancellationToken ct = default) => session.ReadAsync(true, async (db, _) =>
    {
        var preset = await db.DkpAwardPresets.SingleOrDefaultAsync(p => p.Id == presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
        if (!await db.Users.AnyAsync(u => u.Id == targetUserId, ct)) throw new KeyNotFoundException("Player not found.");
        var state = await ReadModels.StateAsync(db, targetUserId, ct);
        var count = state.PresetUsage.GetValueOrDefault(presetId);
        return new DkpPresetUsageDto(presetId, count, preset.MaxApplicationsPerUser, Math.Max(0, preset.MaxApplicationsPerUser - count));
    }, ct);
    public Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<DkpAcquisitionSourceDto>>(false, async (db, actor) =>
    {
        var state = await ReadModels.StateAsync(db, actor.Id, ct);
        var presets = await db.DkpAwardPresets.Where(p => p.IsActive).OrderBy(p => p.Name).ToArrayAsync(ct);
        return presets.Where(p => state.PresetUsage.GetValueOrDefault(p.Id) < p.MaxApplicationsPerUser).Select(p => new DkpAcquisitionSourceDto(p.Id, p.Name, p.Amount, p.Reason, state.PresetUsage.GetValueOrDefault(p.Id), p.MaxApplicationsPerUser, p.MaxApplicationsPerUser - state.PresetUsage.GetValueOrDefault(p.Id))).ToArray();
    }, ct);
}
