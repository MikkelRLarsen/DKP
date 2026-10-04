using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class SoftReserveQueries(DkpDbContext db, DKP.Application.SoftReserves.ISoftReserveSettings settings) : ISoftReserveQueries
{
	public async Task<SoftReserveSummaryDto?> GetForUserAsync(
		string authenticatedDiscordId,
		CancellationToken cancellationToken = default)
	{
		var userId = await db.Users
			.AsNoTracking()
			.Where(user => user.DiscordId == authenticatedDiscordId)
			.Select(user => (Guid?)user.Id)
			.SingleOrDefaultAsync(cancellationToken);

		if (userId is null)
		{
			return null;
		}

		var purchases = await db.SoftReservePurchases
			.AsNoTracking()
			.Where(purchase => purchase.UserId == userId.Value)
			.OrderByDescending(purchase => purchase.CreatedAtUtc)
			.Select(purchase => new SoftReservePurchaseDto(
				purchase.Id,
				purchase.Quantity,
				purchase.DkpCost,
				purchase.CreatedAtUtc,
				purchase.CancelledAtUtc))
			.ToArrayAsync(cancellationToken);

		return new SoftReserveSummaryDto(
			settings.DkpCost,
			settings.MaxReserves,
			purchases.Where(purchase => !purchase.IsCancelled).Sum(purchase => purchase.Quantity),
			[new DkpPurchaseOptionDto("soft-reserve", "Soft Reserves", "Purchase Soft Reserve slots", settings.DkpCost)],
			purchases);
	}
}
