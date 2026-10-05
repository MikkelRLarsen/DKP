using DKP.Application.SoftReserves;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class ShopPurchaseQueries(DkpDbContext db, ISoftReserveSettings softReserveSettings) : IShopPurchaseQueries
{
	public async Task<ActivePurchaseOverviewDto?> GetActiveOverviewAsync(string authenticatedDiscordId, CancellationToken ct = default)
	{
		var userId = await db.Users.AsNoTracking()
			.Where(x => x.DiscordId == authenticatedDiscordId && !x.IsBlocked)
			.Select(x => (Guid?)x.Id)
			.SingleOrDefaultAsync(ct);

		if (userId is null)
		{
			return null;
		}

		var items = await db.ShopItems.AsNoTracking()
			.Where(x => x.IsActive || db.ShopPurchaseProjections.Any(p => p.UserId == userId.Value && p.ShopItemId == x.Id && p.CancelledAtUtc == null))
			.Select(x => new { x.Id, x.Key, x.Name, x.Price, x.MaxPerUser, x.RollBonusValue })
			.ToArrayAsync(ct);

		var activePurchases = await (from purchase in db.ShopPurchaseProjections.AsNoTracking()
				join item in db.ShopItems.AsNoTracking() on purchase.ShopItemId equals item.Id
				where purchase.UserId == userId.Value && purchase.CancelledAtUtc == null
				select new { item.Key, item.RollBonusValue, purchase.Quantity })
			.ToArrayAsync(ct);

		var rollBonus = activePurchases
			.Where(x => x.RollBonusValue is not null)
			.Select(x => x.RollBonusValue!.Value)
			.OrderByDescending(x => x)
			.FirstOrDefault();

		var overviewItems = items
			.Where(x => x.RollBonusValue is null)
			.Select(item =>
			{
				var quantity = activePurchases.Where(x => x.Key == item.Key).Sum(x => x.Quantity);
				var maximum = item.Key == "soft-reserve" ? softReserveSettings.MaxReserves : item.MaxPerUser;
				var remaining = Math.Max(0, maximum - quantity);
				var price = item.Key == "soft-reserve" ? softReserveSettings.DkpCost : item.Price;
				return new ActivePurchaseItemDto(item.Key, item.Name, quantity, maximum, remaining, price, null);
			})
			.OrderBy(x => x.Key == "soft-reserve" ? 0 : 1)
			.ThenBy(x => x.Name)
			.ToArray();

		if (!overviewItems.Any(x => x.Key == "soft-reserve"))
		{
			var softReserveQuantity = activePurchases.Where(x => x.Key == "soft-reserve").Sum(x => x.Quantity);
			overviewItems = overviewItems
				.Append(new ActivePurchaseItemDto("soft-reserve", "Soft Reserve", softReserveQuantity, softReserveSettings.MaxReserves, Math.Max(0, softReserveSettings.MaxReserves - softReserveQuantity), softReserveSettings.DkpCost, null))
				.OrderBy(x => x.Key == "soft-reserve" ? 0 : 1)
				.ThenBy(x => x.Name)
				.ToArray();
		}

		return new ActivePurchaseOverviewDto(overviewItems, rollBonus);
	}
}
