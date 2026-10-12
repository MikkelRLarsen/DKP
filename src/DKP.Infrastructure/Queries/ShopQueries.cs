using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopQueries(QuerySession session) : IShopQueries
{
    public Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken ct = default) => session.ReadAsync(false, (db, _) => ReadModels.ItemsAsync(db, true, ct), ct);
    public Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken ct = default) => session.ReadAsync(true, (db, _) => ReadModels.ItemsAsync(db, false, ct), ct);
    public Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<ShopPurchaseDto>>(false, (db, actor) => ReadModels.PurchasesAsync(db, actor.Id, ct), ct);
    public Task<IReadOnlyList<ShopPurchaseDto>> GetAllPurchasesAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<ShopPurchaseDto>>(true, (db, _) => ReadModels.PurchasesAsync(db, null, ct), ct);
}
