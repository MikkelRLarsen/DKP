using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class ShopPurchaseLimitRepository(CommandUnitOfWork session) : IShopPurchaseLimitRepository
{
    public Task<ShopPurchaseLimit?> FindAsync(Guid id, CancellationToken ct = default) => session.Db.ShopPurchaseLimits.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<ShopPurchaseLimit>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
        => await session.Db.ShopPurchaseLimits.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.SharedKey).ToArrayAsync(ct);
    public Task AddAsync(ShopPurchaseLimit limit, CancellationToken ct = default) { session.Db.ShopPurchaseLimits.Add(limit); return Task.CompletedTask; }
}
