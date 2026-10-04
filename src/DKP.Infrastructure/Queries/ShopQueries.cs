using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopQueries(DkpDbContext db) : IShopQueries
{
	public async Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken ct = default) => await db.ShopItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct);
	public async Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken ct = default) => await db.ShopItems.AsNoTracking().OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct);
	public async Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(Guid? userId = null, CancellationToken ct = default) => await db.ShopPurchases.AsNoTracking().Include(x => x.User).ThenInclude(x => x.Characters).Include(x => x.ShopItem).Where(x => userId == null || x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new ShopPurchaseDto(x.Id,x.UserId,x.User.DiscordName,x.User.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(),x.ShopItemId,x.ShopItem.Name,x.Quantity,x.TotalDkpCost,x.CreatedAtUtc,x.CancelledAtUtc)).ToArrayAsync(ct);
}
