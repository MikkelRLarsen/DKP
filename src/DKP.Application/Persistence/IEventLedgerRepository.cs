using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IEventLedgerRepository
{
    Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveQuantityAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShopPurchaseProjection?> GetPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default);
    Task<DkpEvent> PostAsync(Guid userId, Guid actorId, Guid operationId, DateTime now, ILedgerPayload payload, CancellationToken cancellationToken = default);
}
