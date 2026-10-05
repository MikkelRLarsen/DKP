using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class EventLedgerRepository(DkpDbContext db) : IEventLedgerRepository
{
	public async Task<T> WithUserLocksAsync<T>(IReadOnlyCollection<Guid> userIds, Func<Task<T>> work, CancellationToken ct = default)
	{
		await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
		foreach (var userId in userIds.Distinct().OrderBy(x => x)) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString()}, 0));", ct);
		try { var result = await work(); await transaction.CommitAsync(ct); return result; } catch { await transaction.RollbackAsync(ct); throw; }
	}
	public async Task<int> GetBalanceAsync(Guid userId, CancellationToken ct = default) => await db.DkpBalanceProjections.Where(x => x.UserId == userId).Select(x => (int?)x.Balance).SingleOrDefaultAsync(ct) ?? 0;
	public async Task<int> GetActiveQuantityAsync(Guid userId, Guid itemId, CancellationToken ct = default) => await db.ShopPurchaseProjections.Where(x => x.UserId == userId && x.ShopItemId == itemId && x.CancelledAtUtc == null).Select(x => (int?)x.Quantity).SumAsync(ct) ?? 0;
	public Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken ct = default) => db.ShopPurchaseProjections.AnyAsync(x => x.UserId == userId && x.CancelledAtUtc == null && db.ShopItems.Where(item => item.Id == x.ShopItemId).Select(item => item.RollBonusValue).FirstOrDefault() != null, ct);
	public Task<ShopPurchaseProjection?> GetPurchaseAsync(Guid id, CancellationToken ct = default) => db.ShopPurchaseProjections.SingleOrDefaultAsync(x => x.PurchaseId == id, ct);
	public Task AppendAsync(IReadOnlyCollection<DkpEvent> events, CancellationToken ct = default) { db.DkpEvents.AddRange(events); return Task.CompletedTask; }
	public async Task<long> GetNextSequenceAsync(string aggregateType, Guid aggregateId, CancellationToken ct = default) => (await db.DkpEvents.Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId).Select(x => (long?)x.Sequence).MaxAsync(ct) ?? -1) + 1;
	public async Task ApplyBalanceAsync(Guid userId, int amount, Guid eventId, DateTime occurredAtUtc, CancellationToken ct = default) { var projection = await db.DkpBalanceProjections.SingleOrDefaultAsync(x => x.UserId == userId, ct); if (projection is null) { projection = new DkpBalanceProjection(userId); db.DkpBalanceProjections.Add(projection); } projection.Apply(amount, eventId, occurredAtUtc); }
	public Task AddPurchaseProjectionAsync(ShopPurchaseProjection projection, CancellationToken ct = default) { db.ShopPurchaseProjections.Add(projection); return Task.CompletedTask; }
	public async Task CancelPurchaseProjectionAsync(Guid purchaseId, DateTime occurredAtUtc, CancellationToken ct = default) { var projection = await db.ShopPurchaseProjections.SingleOrDefaultAsync(x => x.PurchaseId == purchaseId, ct) ?? throw new KeyNotFoundException("Purchase projection not found."); projection.Cancel(occurredAtUtc); }
	public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
