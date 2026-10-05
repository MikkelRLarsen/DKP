using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopQueries(DkpDbContext db, DKP.Application.SoftReserves.ISoftReserveSettings softReserveSettings) : IShopQueries
{
	public async Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken ct = default) => ApplySoftReserveLimit(await db.ShopItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct));
	public async Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken ct = default) => ApplySoftReserveLimit(await db.ShopItems.AsNoTracking().OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct));
	private IReadOnlyList<ShopItemDto> ApplySoftReserveLimit(IReadOnlyList<ShopItemDto> items) => items.Select(x => x.Key == "soft-reserve" ? x with { MaxPerUser = softReserveSettings.MaxReserves, Price = softReserveSettings.DkpCost } : x).ToArray();
	public async Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(Guid? userId = null, CancellationToken ct = default) => await (from purchase in db.ShopPurchaseProjections.AsNoTracking() join user in db.Users.AsNoTracking() on purchase.UserId equals user.Id join item in db.ShopItems.AsNoTracking() on purchase.ShopItemId equals item.Id where userId == null || purchase.UserId == userId orderby purchase.CreatedAtUtc descending select new ShopPurchaseDto(purchase.PurchaseId,purchase.UserId,user.DiscordName,user.Characters.Where(c=>c.IsMain).Select(c=>c.FirstName+" "+c.LastName).FirstOrDefault(),purchase.ShopItemId,item.Name,purchase.Quantity,purchase.TotalDkpCost,purchase.CreatedAtUtc,purchase.CancelledAtUtc)).ToArrayAsync(ct);
}
