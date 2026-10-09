using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotShopQueries
{
    Task<IReadOnlyList<ShopItemDto>?> GetActiveItemsAsync(string discordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopPurchaseDto>?> GetPurchasesAsync(string discordId, CancellationToken cancellationToken = default);
}
