using System.Text.Json;
using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class EventProjectionRebuilder(DkpDbContext db) : IEventProjectionRebuilder
{
	public async Task RebuildAsync(CancellationToken ct = default)
	{
		db.DkpBalanceProjections.RemoveRange(await db.DkpBalanceProjections.ToArrayAsync(ct));
		db.ShopPurchaseProjections.RemoveRange(await db.ShopPurchaseProjections.ToArrayAsync(ct));
		await db.SaveChangesAsync(ct);
		foreach (var ev in await db.DkpEvents.AsNoTracking().OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id).ToArrayAsync(ct))
		{
			using var json = JsonDocument.Parse(ev.Payload); var root = json.RootElement;
			if (ev.EventType is "DkpCredited" or "DkpDebited")
			{
				var amount = root.GetProperty("amount").GetInt32(); var projection = await db.DkpBalanceProjections.SingleOrDefaultAsync(x => x.UserId == ev.UserId, ct) ?? new DkpBalanceProjection(ev.UserId); if (projection.LastEventId is null) db.DkpBalanceProjections.Add(projection); projection.Apply(amount, ev.Id, ev.OccurredAtUtc);
			}
			else if (ev.EventType == "ShopPurchaseCreated")
			{
				var userId = root.GetProperty("userId").GetGuid();
				var itemId = root.GetProperty("itemId").GetGuid();
				var purchase = new ShopPurchaseProjection(root.GetProperty("purchaseId").GetGuid(), userId, itemId, root.GetProperty("quantity").GetInt32(), root.GetProperty("totalCost").GetInt32(), root.GetProperty("actorId").GetGuid(), ev.OccurredAtUtc);
				db.ShopPurchaseProjections.Add(purchase);
				var rollBonusValue = await db.ShopItems.Where(x => x.Id == itemId).Select(x => x.RollBonusValue).SingleOrDefaultAsync(ct);
				if (rollBonusValue is not null)
				{
					var user = await db.Users.SingleAsync(x => x.Id == userId, ct);
					user.SetRollBonus(rollBonusValue.Value);
				}
			}
			else if (ev.EventType == "ShopPurchaseCancelled")
			{
				var id = root.GetProperty("purchaseId").GetGuid();
				var purchase = await db.ShopPurchaseProjections.SingleAsync(x => x.PurchaseId == id, ct);
				purchase.Cancel(ev.OccurredAtUtc);
				var itemId = await db.ShopPurchaseProjections.Where(x => x.PurchaseId == id).Select(x => x.ShopItemId).SingleAsync(ct);
				var rollBonusValue = await db.ShopItems.Where(x => x.Id == itemId).Select(x => x.RollBonusValue).SingleOrDefaultAsync(ct);
				if (rollBonusValue is not null)
				{
					var userId = await db.ShopPurchaseProjections.Where(x => x.PurchaseId == id).Select(x => x.UserId).SingleAsync(ct);
					var user = await db.Users.SingleAsync(x => x.Id == userId, ct);
					user.SetRollBonus(0);
				}
			}
		}
		await db.SaveChangesAsync(ct);
	}
}
