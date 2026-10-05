using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IShopQueries
{
	Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> GetAllPurchasesAsync(CancellationToken cancellationToken = default);
}
