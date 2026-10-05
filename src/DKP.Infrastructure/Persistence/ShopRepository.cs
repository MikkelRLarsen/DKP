using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class ShopRepository(DkpDbContext db) : IShopRepository
{
	public Task<ShopItem?> FindItemAsync(Guid id, CancellationToken ct = default) => db.ShopItems.SingleOrDefaultAsync(x => x.Id == id, ct);
	public Task<ShopPurchase?> FindPurchaseAsync(Guid id, CancellationToken ct = default) => db.ShopPurchases.SingleOrDefaultAsync(x => x.Id == id, ct);
	public async Task<int> GetActiveQuantityAsync(Guid userId, Guid itemId, CancellationToken ct = default) => await db.ShopPurchases.Where(x => x.UserId == userId && x.ShopItemId == itemId && x.CancelledAtUtc == null).Select(x => (int?)x.Quantity).SumAsync(ct) ?? 0;
	public Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken ct = default) => db.ShopPurchases.AnyAsync(x => x.UserId == userId && x.CancelledAtUtc == null && x.ShopItem.RollBonusValue != null, ct);
	public Task AddItemAsync(ShopItem item, CancellationToken ct = default) { db.ShopItems.Add(item); return Task.CompletedTask; }
	public Task AddPurchaseAsync(ShopPurchase purchase, CancellationToken ct = default) { db.ShopPurchases.Add(purchase); return Task.CompletedTask; }
	public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
