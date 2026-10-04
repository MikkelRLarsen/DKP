using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IShopCommands
{
	Task<ShopItemDto> CreateItemAsync(string actorDiscordId, ShopItemInput input, CancellationToken cancellationToken = default);
	Task<ShopItemDto> UpdateItemAsync(string actorDiscordId, Guid itemId, ShopItemInput input, CancellationToken cancellationToken = default);
	Task SetActiveAsync(string actorDiscordId, Guid itemId, bool active, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> PurchaseAsync(string actorDiscordId, ShopPurchaseRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ShopPurchaseDto>> PurchaseForUsersAsync(string actorDiscordId, AdminShopPurchaseRequest request, CancellationToken cancellationToken = default);
	Task CancelAsync(string actorDiscordId, Guid purchaseId, CancellationToken cancellationToken = default);
}
