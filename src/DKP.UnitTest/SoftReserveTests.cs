using DKP.Application.Persistence;
using DKP.Application.SoftReserves;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DKP.UnitTest;

public sealed class SoftReserveTests
{
	[Fact]
	public async Task Purchase_creates_reserve_and_negative_dkp_transaction()
	{
		await using var db = CreateDatabase();
		var user = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		db.Users.Add(user);
		db.DkpTransactions.Add(new DkpTransaction(user.Id, 50, "Raid", user.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();
		var service = CreateService(db, cost: 10, maxReserves: 2);

		var result = await service.PurchaseAsync("member", new PurchaseSoftReserveRequest(1));

		Assert.Equal(1, result.Quantity);
		Assert.Equal(10, result.DkpCost);
		Assert.Single(db.SoftReservePurchases);
		var transaction = Assert.Single(db.DkpTransactions.Where(item => item.Amount < 0));
		Assert.Equal(-10, transaction.Amount);
		Assert.Equal("Soft Reserve purchase x1", transaction.Reason);
	}

	[Fact]
	public async Task Purchase_rejects_when_maximum_quantity_is_exceeded()
	{
		await using var db = CreateDatabase();
		var user = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		user.SoftReservePurchases.Add(new SoftReservePurchase(user.Id, 2, 20, DateTime.UtcNow));
		db.Users.Add(user);
		db.DkpTransactions.Add(new DkpTransaction(user.Id, 50, "Raid", user.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();
		var service = CreateService(db, cost: 10, maxReserves: 2);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			service.PurchaseAsync("member", new PurchaseSoftReserveRequest(1)));
	}

	[Fact]
	public async Task Purchase_rejects_insufficient_balance_and_invalid_number()
	{
		await using var db = CreateDatabase();
		var user = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		db.Users.Add(user);
		db.DkpTransactions.Add(new DkpTransaction(user.Id, 5, "Raid", user.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();
		var service = CreateService(db, cost: 10, maxReserves: 2);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			service.PurchaseAsync("member", new PurchaseSoftReserveRequest(1)));
		await Assert.ThrowsAsync<ArgumentException>(() =>
			service.PurchaseAsync("member", new PurchaseSoftReserveRequest(0)));
	}

	[Fact]
	public async Task Cancellation_marks_purchase_cancelled_and_refunds_dkp()
	{
		await using var db = CreateDatabase();
		var user = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		db.Users.Add(user);
		db.DkpTransactions.Add(new DkpTransaction(user.Id, 50, "Raid", user.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();
		var service = CreateService(db, cost: 10, maxReserves: 2);

		var purchase = await service.PurchaseAsync("member", new PurchaseSoftReserveRequest(2));
		var cancelled = await service.CancelAsync("member", purchase.Id);

		Assert.NotNull(cancelled.CancelledAtUtc);
		Assert.True(cancelled.IsCancelled);
		Assert.Equal(50, db.DkpTransactions.Sum(transaction => transaction.Amount));
	}

	[Fact]
	public async Task Query_returns_only_authenticated_users_soft_reserves()
	{
		await using var db = CreateDatabase();
		var first = new User("first", "First", null, UserRole.Member, DateTime.UtcNow);
		var second = new User("second", "Second", null, UserRole.Member, DateTime.UtcNow);
		first.SoftReservePurchases.Add(new SoftReservePurchase(first.Id, 1, 10, DateTime.UtcNow));
		second.SoftReservePurchases.Add(new SoftReservePurchase(second.Id, 1, 10, DateTime.UtcNow));
		db.Users.AddRange(first, second);
		await db.SaveChangesAsync();

		var result = await new SoftReserveQueries(db, new FakeSettings(10, 2)).GetForUserAsync("first");

		Assert.NotNull(result);
		Assert.Equal(10, result.DkpCost);
		Assert.Single(result.Purchases);
	}

	private static SoftReserveCommandService CreateService(DkpDbContext db, int cost, int maxReserves)
		=> new(
			new UserRepository(db),
			new SoftReservePurchaseRepository(db),
			new DkpTransactionRepository(db),
			new FakeSettings(cost, maxReserves),
			TimeProvider.System);

	private static DkpDbContext CreateDatabase()
		=> new(new DbContextOptionsBuilder<DkpDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options);

	private sealed record FakeSettings(int DkpCost, int MaxReserves) : ISoftReserveSettings;
}
