using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class ShopPurchaseLimitQueries(QuerySession session) : IShopPurchaseLimitQueries
{
    public Task<IReadOnlyList<ShopPurchaseLimitDto>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<ShopPurchaseLimitDto>>(true, async (db, _) => await db.ShopPurchaseLimits.AsNoTracking()
            .Where(x => !activeOnly || x.IsActive).OrderBy(x => x.SharedKey)
            .Select(x => new ShopPurchaseLimitDto(x.Id, x.SharedKey, x.MaxPerUser, x.MaxActivePerUser, x.MaxPerDay, x.MaxPerWeek, x.MaxPerMonth, x.IsActive)).ToArrayAsync(ct), ct);
}
