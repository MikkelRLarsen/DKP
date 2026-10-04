using DKP.Domain;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DKP.UnitTest;

public sealed class DkpQueryTests
{
	[Fact]
	public async Task History_calculates_balance_from_positive_and_negative_transactions()
	{
		var user = new User("user-1", "Member", null, UserRole.Member, DateTime.UtcNow);
		var officer = new User("officer-1", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.AddRange(user, officer);
		db.DkpTransactions.AddRange(
			new DkpTransaction(user.Id, 50, "Raid", officer.Id, DateTime.UtcNow.AddMinutes(-2)),
			new DkpTransaction(user.Id, -20, "Correction", officer.Id, DateTime.UtcNow.AddMinutes(-1)));
		await db.SaveChangesAsync();

		var result = await new DkpQueries(db).GetHistoryAsync(user.DiscordId);

		Assert.NotNull(result);
		Assert.Equal(30, result.Balance.Amount);
		Assert.Equal(2, result.Transactions.Count);
	}

	[Fact]
	public async Task History_is_sorted_newest_first()
	{
		var user = new User("user-1", "Member", null, UserRole.Member, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.Add(user);
		db.DkpTransactions.AddRange(
			new DkpTransaction(user.Id, 10, "Older", user.Id, DateTime.UtcNow.AddDays(-1)),
			new DkpTransaction(user.Id, 20, "Newer", user.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();

		var result = await new DkpQueries(db).GetHistoryAsync(user.DiscordId);

		Assert.NotNull(result);
		Assert.Equal("Newer", result.Transactions[0].Reason);
	}

	[Fact]
	public async Task History_is_limited_to_authenticated_user()
	{
		var firstUser = new User("user-1", "First", null, UserRole.Member, DateTime.UtcNow);
		var secondUser = new User("user-2", "Second", null, UserRole.Member, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.AddRange(firstUser, secondUser);
		db.DkpTransactions.AddRange(
			new DkpTransaction(firstUser.Id, 10, "First transaction", firstUser.Id, DateTime.UtcNow),
			new DkpTransaction(secondUser.Id, 99, "Other transaction", secondUser.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();

		var result = await new DkpQueries(db).GetHistoryAsync(firstUser.DiscordId);

		Assert.NotNull(result);
		var transaction = Assert.Single(result.Transactions);
		Assert.Equal("First transaction", transaction.Reason);
		Assert.Equal(10, result.Balance.Amount);
	}

	[Fact]
	public async Task User_without_transactions_has_zero_balance_and_empty_history()
	{
		var user = new User("user-1", "Member", null, UserRole.Member, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.Add(user);
		await db.SaveChangesAsync();

		var result = await new DkpQueries(db).GetHistoryAsync(user.DiscordId);

		Assert.NotNull(result);
		Assert.Equal(0, result.Balance.Amount);
		Assert.Empty(result.Transactions);
	}

	[Fact]
	public async Task Dashboard_uses_calculated_dkp_balance()
	{
		var user = new User("user-1", "Member", null, UserRole.Member, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.Add(user);
		db.DkpTransactions.AddRange(
			new DkpTransaction(user.Id, 75, "Raid", user.Id, DateTime.UtcNow.AddMinutes(-2)),
			new DkpTransaction(user.Id, -25, "Correction", user.Id, DateTime.UtcNow.AddMinutes(-1)));
		await db.SaveChangesAsync();

		var result = await new AccountQueries(db).GetDashboardAsync(user.DiscordId);

		Assert.NotNull(result);
		Assert.Equal(50, result.DkpBalance);
	}

	private static DkpDbContext CreateDatabase()
	{
		return new DkpDbContext(new DbContextOptionsBuilder<DkpDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options);
	}
}
