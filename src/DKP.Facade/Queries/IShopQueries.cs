using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IShopQueries
{
	Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(string authenticatedDiscordId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> GetAllPurchasesAsync(string officerDiscordId, CancellationToken cancellationToken = default);
}
