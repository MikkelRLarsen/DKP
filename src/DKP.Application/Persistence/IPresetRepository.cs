using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IPresetRepository
{
    Task<DkpAwardPreset?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUsageCountAsync(Guid presetId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(DkpAwardPreset preset, CancellationToken cancellationToken = default);
}
