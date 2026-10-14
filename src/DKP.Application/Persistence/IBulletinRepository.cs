using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IBulletinRepository
{
    Task<BulletinItem?> FindItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BulletinClaim?> FindClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetReservedQuantityAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<int> GetPendingQuantityAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveClaimAsync(Guid itemId, Guid userId, CancellationToken cancellationToken = default);
    Task AddItemAsync(BulletinItem item, CancellationToken cancellationToken = default);
    Task AddClaimAsync(BulletinClaim claim, CancellationToken cancellationToken = default);
}
