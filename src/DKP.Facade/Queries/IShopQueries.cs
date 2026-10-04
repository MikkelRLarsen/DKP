using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IShopQueries
{
	Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}
