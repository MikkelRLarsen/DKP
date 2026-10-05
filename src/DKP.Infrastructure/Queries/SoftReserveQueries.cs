using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class SoftReserveQueries(DkpDbContext db, DKP.Application.SoftReserves.ISoftReserveSettings settings) : ISoftReserveQueries
{
	public async Task<SoftReserveSummaryDto?> GetForUserAsync(string discordId, CancellationToken ct = default)
	{
		var userId=await db.Users.AsNoTracking().Where(x=>x.DiscordId==discordId).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);if(userId is null)return null;
		var purchases=await (from purchase in db.ShopPurchaseProjections.AsNoTracking() join item in db.ShopItems.AsNoTracking() on purchase.ShopItemId equals item.Id where purchase.UserId==userId.Value&&item.Key=="soft-reserve" orderby purchase.CreatedAtUtc descending select new SoftReservePurchaseDto(purchase.PurchaseId,purchase.Quantity,purchase.TotalDkpCost,purchase.CreatedAtUtc,purchase.CancelledAtUtc)).ToArrayAsync(ct);
		if (purchases.Length == 0) purchases = await db.SoftReservePurchases.AsNoTracking().Where(x=>x.UserId==userId.Value).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new SoftReservePurchaseDto(x.Id,x.Quantity,x.DkpCost,x.CreatedAtUtc,x.CancelledAtUtc)).ToArrayAsync(ct);
		return new SoftReserveSummaryDto(settings.DkpCost,settings.MaxReserves,purchases.Where(x=>!x.IsCancelled).Sum(x=>x.Quantity),[new DkpPurchaseOptionDto("soft-reserve","Soft Reserves","Purchase Soft Reserve slots",settings.DkpCost)],purchases);
	}
}
