using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class SoftReservePurchaseRepository(DkpDbContext db) : ISoftReservePurchaseRepository
{
	public async Task<int> GetActiveQuantityAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var quantities = await db.SoftReservePurchases
			.AsNoTracking()
			.Where(purchase => purchase.UserId == userId && purchase.CancelledAtUtc == null)
			.Select(purchase => purchase.Quantity)
			.ToArrayAsync(cancellationToken);

		return quantities.Sum();
	}

	public Task<SoftReservePurchase?> FindForUserAsync(Guid purchaseId, Guid userId, CancellationToken cancellationToken = default)
		=> db.SoftReservePurchases.SingleOrDefaultAsync(
			purchase => purchase.Id == purchaseId && purchase.UserId == userId,
			cancellationToken);

	public Task AddAsync(SoftReservePurchase purchase, CancellationToken cancellationToken = default)
	{
		db.SoftReservePurchases.Add(purchase);
		return Task.CompletedTask;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> db.SaveChangesAsync(cancellationToken);
}
