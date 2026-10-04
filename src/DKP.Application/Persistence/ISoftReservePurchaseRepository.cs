using DKP.Domain;

namespace DKP.Application.Persistence;

public interface ISoftReservePurchaseRepository
{
	Task<int> GetActiveQuantityAsync(Guid userId, CancellationToken cancellationToken = default);
	Task<SoftReservePurchase?> FindForUserAsync(Guid purchaseId, Guid userId, CancellationToken cancellationToken = default);
	Task AddAsync(SoftReservePurchase purchase, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
