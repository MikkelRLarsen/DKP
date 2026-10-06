using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IDkpAwardRequestRepository
{
    Task<DkpAwardRequest?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPendingAsync(Guid userId, Guid? presetId, Guid? achievementId, CancellationToken cancellationToken = default);
    Task AddAsync(DkpAwardRequest request, CancellationToken cancellationToken = default);
}
