using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class ShopRepository(CommandUnitOfWork session) : IShopRepository
{
    public Task<ShopItem?> FindItemAsync(Guid id, CancellationToken ct = default) => session.Db.ShopItems.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddItemAsync(ShopItem item, CancellationToken ct = default) { session.Db.ShopItems.Add(item); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct = default) => session.Db.SaveChangesAsync(ct);
}
