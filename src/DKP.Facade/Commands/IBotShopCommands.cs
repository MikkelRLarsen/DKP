using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotShopCommands
{
    Task<ShopPurchaseDto> PurchaseAsync(string discordId, ShopPurchaseRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(string discordId, Guid purchaseId, CancellationToken cancellationToken = default);
}
