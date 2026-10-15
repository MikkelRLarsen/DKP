using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IShopPurchaseLimitQueries
{
    Task<IReadOnlyList<ShopPurchaseLimitDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
}
