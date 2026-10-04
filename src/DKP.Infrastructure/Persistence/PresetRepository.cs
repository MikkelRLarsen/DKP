using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class PresetRepository(DkpDbContext db) : IPresetRepository
{
	public Task<DkpAwardPreset?> FindAsync(Guid id, CancellationToken ct = default) => db.DkpAwardPresets.SingleOrDefaultAsync(x => x.Id == id, ct);
	public Task<int> GetUsageCountAsync(Guid presetId, Guid userId, CancellationToken ct = default) => db.DkpAwardPresetApplications.CountAsync(x => x.PresetId == presetId && x.UserId == userId, ct);
	public Task AddAsync(DkpAwardPreset preset, CancellationToken ct = default) { db.DkpAwardPresets.Add(preset); return Task.CompletedTask; }
	public Task AddApplicationAsync(DkpAwardPresetApplication app, CancellationToken ct = default) { db.DkpAwardPresetApplications.Add(app); return Task.CompletedTask; }
	public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
