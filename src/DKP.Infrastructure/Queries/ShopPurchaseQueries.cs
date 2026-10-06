using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopPurchaseQueries(QuerySession session) : IShopPurchaseQueries
{
    public Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(CancellationToken ct = default) => session.ReadAsync<ActivePurchaseOverviewDto?>(false, async (db, actor) =>
    {
        var state = await ReadModels.StateAsync(db, actor.Id, ct);
        var items = await db.ShopItems.OrderBy(i => i.Name).ThenBy(i => i.Id).ToArrayAsync(ct);
        var result = items.Where(i => i.Key == "soft-reserve" || state.Purchases.Values.Any(p => p.ShopItemId == i.Id && p.CancelledAtUtc is null)).Select(i =>
        {
            var owned = state.Purchases.Values.Where(p => p.ShopItemId == i.Id && p.CancelledAtUtc is null).ToArray();
            var quantity = owned.Sum(p => p.Quantity);
            return new ActivePurchaseItemDto(i.Key, i.Name, quantity, i.MaxPerUser, Math.Max(0, i.MaxPerUser - quantity), i.Price, owned.FirstOrDefault()?.RollBonusValue ?? i.RollBonusValue);
        }).ToArray();
        return new ActivePurchaseOverviewDto(result, state.Purchases.Values.Where(p => p.CancelledAtUtc is null).Sum(p => p.RollBonusValue ?? 0));
    }, ct);
}
