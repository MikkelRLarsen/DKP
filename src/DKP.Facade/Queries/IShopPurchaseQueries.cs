using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IShopPurchaseQueries
{
	Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopItemAvailabilityDto>> GetAvailabilityAsync(CancellationToken cancellationToken = default);
}
