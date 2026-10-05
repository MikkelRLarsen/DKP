using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Domain;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class ShopQueries(DkpDbContext db, DKP.Application.SoftReserves.ISoftReserveSettings softReserveSettings) : IShopQueries
{
	public async Task<IReadOnlyList<ShopItemDto>> GetActiveItemsAsync(CancellationToken ct = default) => ApplySoftReserveLimit(await db.ShopItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct));
	public async Task<IReadOnlyList<ShopItemDto>> GetAllItemsAsync(CancellationToken ct = default) => ApplySoftReserveLimit(await db.ShopItems.AsNoTracking().OrderBy(x => x.Name).Select(x => new ShopItemDto(x.Id,x.Key,x.Name,x.Description,x.Price,x.MaxPerUser,x.IsActive)).ToArrayAsync(ct));
	private IReadOnlyList<ShopItemDto> ApplySoftReserveLimit(IReadOnlyList<ShopItemDto> items) => items.Select(x => x.Key == "soft-reserve" ? x with { MaxPerUser = softReserveSettings.MaxReserves, Price = softReserveSettings.DkpCost } : x).ToArray();
	public async Task<IReadOnlyList<ShopPurchaseDto>> GetPurchasesAsync(string authenticatedDiscordId, CancellationToken ct = default)
	{
		var userId = await db.Users.AsNoTracking().Where(x => x.DiscordId == authenticatedDiscordId && !x.IsBlocked).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
		return userId is null ? [] : await PurchaseQuery(userId.Value).ToArrayAsync(ct);
	}

	public async Task<IReadOnlyList<ShopPurchaseDto>> GetAllPurchasesAsync(string officerDiscordId, CancellationToken ct = default)
	{
		var isOfficer = await db.Users.AsNoTracking().AnyAsync(x => x.DiscordId == officerDiscordId && !x.IsBlocked && x.Role == UserRole.Officer, ct);
		if (!isOfficer) throw new UnauthorizedAccessException("Only Officers can view all shop purchases.");
		return await PurchaseQuery(null).ToArrayAsync(ct);
	}

	private IQueryable<ShopPurchaseDto> PurchaseQuery(Guid? userId) =>
		from purchase in db.ShopPurchaseProjections.AsNoTracking()
		join user in db.Users.AsNoTracking() on purchase.UserId equals user.Id
		join item in db.ShopItems.AsNoTracking() on purchase.ShopItemId equals item.Id
		where userId == null || purchase.UserId == userId.Value
		orderby purchase.CreatedAtUtc descending
		select new ShopPurchaseDto(purchase.PurchaseId, purchase.UserId, user.DiscordName, user.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(), purchase.ShopItemId, item.Name, purchase.Quantity, purchase.TotalDkpCost, purchase.CreatedAtUtc, purchase.CancelledAtUtc);
}
