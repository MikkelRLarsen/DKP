using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class PresetRepository(CommandUnitOfWork session) : IPresetRepository
{
    public Task<DkpAwardPreset?> FindAsync(Guid id, CancellationToken ct = default) => session.Db.DkpAwardPresets.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<int> GetUsageCountAsync(Guid presetId, Guid userId, CancellationToken ct = default) => session.Db.DkpAwardPresetApplications.CountAsync(x => x.PresetId == presetId && x.UserId == userId, ct);
    public Task AddAsync(DkpAwardPreset preset, CancellationToken ct = default) { session.Db.DkpAwardPresets.Add(preset); return Task.CompletedTask; }
}
