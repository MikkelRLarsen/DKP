using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IEventLedgerRepository
{
	Task<T> WithUserLocksAsync<T>(IReadOnlyCollection<Guid> userIds, Func<Task<T>> work, CancellationToken cancellationToken = default);
	Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
	Task<int> GetActiveQuantityAsync(Guid userId, Guid shopItemId, CancellationToken cancellationToken = default);
	Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken cancellationToken = default);
	Task<ShopPurchaseProjection?> GetPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default);
	Task AppendAsync(IReadOnlyCollection<DkpEvent> events, CancellationToken cancellationToken = default);
	Task<long> GetNextSequenceAsync(string aggregateType, Guid aggregateId, CancellationToken cancellationToken = default);
	Task ApplyBalanceAsync(Guid userId, int amount, Guid eventId, DateTime occurredAtUtc, CancellationToken cancellationToken = default);
	Task AddPurchaseProjectionAsync(ShopPurchaseProjection projection, CancellationToken cancellationToken = default);
	Task CancelPurchaseProjectionAsync(Guid purchaseId, DateTime occurredAtUtc, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
