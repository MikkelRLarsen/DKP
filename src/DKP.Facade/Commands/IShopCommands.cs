using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IShopCommands
{
	Task<ShopItemDto> CreateItemAsync(ShopItemInput input, CancellationToken cancellationToken = default);
	Task<ShopItemDto> UpdateItemAsync(Guid itemId, ShopItemInput input, CancellationToken cancellationToken = default);
	Task SetActiveAsync(Guid itemId, bool active, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> PurchaseAsync(ShopPurchaseRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> PurchaseForUsersAsync(AdminShopPurchaseRequest request, CancellationToken cancellationToken = default);
	Task CancelAsync(Guid purchaseId, CancellationToken cancellationToken = default);
	Task MarkUsedAsync(Guid purchaseId, CancellationToken cancellationToken = default);
	Task RevertUsedAsync(Guid purchaseId, CancellationToken cancellationToken = default);
}
