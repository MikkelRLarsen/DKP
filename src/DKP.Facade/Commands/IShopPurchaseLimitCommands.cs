using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IShopPurchaseLimitCommands
{
    Task<ShopPurchaseLimitDto> CreateAsync(ShopPurchaseLimitInput input, CancellationToken cancellationToken = default);
    Task<ShopPurchaseLimitDto> UpdateAsync(Guid id, ShopPurchaseLimitInput input, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
}
