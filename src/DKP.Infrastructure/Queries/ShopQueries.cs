using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopQueries(QuerySession session) : IShopQueries
{
    public Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<ShopItemDto>>(false, async (db, _) => await ReadModels.Items(db, activeOnly: true).ToArrayAsync(ct), ct);
    public Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<ShopItemDto>>(true, async (db, _) => await ReadModels.Items(db).ToArrayAsync(ct), ct);
    public Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<ShopPurchaseDto>>(false, async (db, actor) => await ReadModels.Purchases(db, actor.Id).ToArrayAsync(ct), ct);
    public Task<IReadOnlyList<ShopPurchaseDto>> GetAllPurchasesAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<ShopPurchaseDto>>(true, async (db, _) => await ReadModels.Purchases(db).ToArrayAsync(ct), ct);
}
