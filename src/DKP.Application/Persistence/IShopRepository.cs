using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IShopRepository
{
	Task<ShopItem?> FindItemAsync(Guid id, CancellationToken cancellationToken = default);
	Task<ShopPurchase?> FindPurchaseAsync(Guid id, CancellationToken cancellationToken = default);
	Task<int> GetActiveQuantityAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
	Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken cancellationToken = default);
	Task AddItemAsync(ShopItem item, CancellationToken cancellationToken = default);
	Task AddPurchaseAsync(ShopPurchase purchase, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
