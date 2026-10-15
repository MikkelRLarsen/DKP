using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IShopPurchaseLimitRepository
{
    Task<ShopPurchaseLimit?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopPurchaseLimit>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task AddAsync(ShopPurchaseLimit limit, CancellationToken cancellationToken = default);
}
