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
        int? remaining = preset.MaxApplicationsPerUser is int maximum ? Math.Max(0, maximum - count) : null;
        return new DkpPresetUsageDto(presetId, count, preset.MaxApplicationsPerUser, remaining);
    }, ct);
    public Task<IReadOnlyList<DkpAcquisitionSourceDto>> GetAvailableSourcesAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<DkpAcquisitionSourceDto>>(false, async (db, actor) =>
    {
        var state = await ReadModels.StateAsync(db, actor.Id, ct);
        var presets = await db.DkpAwardPresets.Where(p => p.IsActive).OrderBy(p => p.Name).ToArrayAsync(ct);
        var pendingByPreset = await db.DkpAwardRequests.AsNoTracking()
            .Where(x => x.UserId == actor.Id && x.Status == DKP.Domain.DkpAwardRequestStatus.Pending && x.PresetId != null)
            .GroupBy(x => x.PresetId!.Value)
            .Select(x => new { PresetId = x.Key, Quantity = x.Sum(r => r.Quantity) })
            .ToDictionaryAsync(x => x.PresetId, x => x.Quantity, ct);
        return presets.Select(p =>
        {
            var approved = state.PresetUsage.GetValueOrDefault(p.Id);
            var pending = pendingByPreset.GetValueOrDefault(p.Id);
            int? remaining = p.MaxApplicationsPerUser is int maximum ? Math.Max(0, maximum - approved - pending) : null;
            return new DkpAcquisitionSourceDto(p.Id, p.Name, p.Amount, p.Reason, approved + pending, p.MaxApplicationsPerUser, remaining);
        }).Where(x => x.Remaining is null or > 0).ToArray();
    }, ct);
}
