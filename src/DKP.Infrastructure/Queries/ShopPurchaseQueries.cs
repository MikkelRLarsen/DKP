using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopPurchaseQueries(QuerySession session) : IShopPurchaseQueries
{
    public Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(CancellationToken ct = default)
        => session.ReadAsync<ActivePurchaseOverviewDto?>(false, async (db, actor) =>
        {
            var active = await db.ShopPurchaseProjections.Where(p => p.UserId == actor.Id && p.CancelledAtUtc == null).ToListAsync(ct);
            var items = await db.ShopItems.OrderBy(i => i.Name).ThenBy(i => i.Id).ToListAsync(ct);
            var result = items.Where(i => i.Key == "soft-reserve" || active.Any(p => p.ShopItemId == i.Id))
                .Select(i =>
                {
                    var owned = active.Where(p => p.ShopItemId == i.Id).ToArray();
                    var quantity = owned.Sum(p => p.Quantity);
                    return new ActivePurchaseItemDto(i.Key, i.Name, quantity, i.MaxPerUser,
                        Math.Max(0, i.MaxPerUser - quantity), i.Price, owned.FirstOrDefault()?.RollBonusValue ?? i.RollBonusValue);
                }).ToArray();
            return new(result, active.Sum(p => p.RollBonusValue ?? 0));
        }, ct);
}
