using DKP.Application.DkpTransactions;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using DKP.Facade.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DKP.UnitTest;

public sealed class DkpTransactionCommandTests
{
	[Fact]
	public async Task Officer_can_add_dkp_with_audit_user()
	{
		var officer = new User("officer", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		var target = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		var users = new FakeUserRepository(officer, target);
		var transactions = new FakeDkpTransactionRepository();
		var service = new DkpTransactionCommandService(users, transactions);

		var result = await service.AddAsync("officer", new CreateDkpTransactionRequest(target.Id, 50, "Raid"));

		Assert.Equal(50, result.Amount);
		Assert.Equal("Officer", result.CreatedByDiscordName);
		Assert.Single(transactions.Transactions);
		Assert.Equal(officer.Id, transactions.Transactions[0].CreatedByUserId);
	}

	[Fact]
	public async Task Officer_can_remove_dkp_as_negative_transaction()
	{
		var officer = new User("officer", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		var target = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		var service = new DkpTransactionCommandService(
			new FakeUserRepository(officer, target),
			new FakeDkpTransactionRepository());

		var result = await service.RemoveAsync("officer", new CreateDkpTransactionRequest(target.Id, 20, "Correction"));

		Assert.Equal(-20, result.Amount);
	}

	[Fact]
	public async Task Member_cannot_create_dkp_transaction()
	{
		var member = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		var target = new User("target", "Target", null, UserRole.Member, DateTime.UtcNow);
		var transactions = new FakeDkpTransactionRepository();
		var service = new DkpTransactionCommandService(new FakeUserRepository(member, target), transactions);

		await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
			service.AddAsync("member", new CreateDkpTransactionRequest(target.Id, 10, "Nope")));
		Assert.Empty(transactions.Transactions);
	}

	[Fact]
	public async Task Invalid_amount_and_reason_are_rejected()
	{
		var officer = new User("officer", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		var target = new User("target", "Target", null, UserRole.Member, DateTime.UtcNow);
		var service = new DkpTransactionCommandService(
			new FakeUserRepository(officer, target),
			new FakeDkpTransactionRepository());

		await Assert.ThrowsAsync<ArgumentException>(() =>
			service.AddAsync("officer", new CreateDkpTransactionRequest(target.Id, 0, "Raid")));
		await Assert.ThrowsAsync<ArgumentException>(() =>
			service.AddAsync("officer", new CreateDkpTransactionRequest(target.Id, 10, "  ")));
	}

	[Fact]
	public async Task Created_transaction_is_visible_in_target_history_and_balance()
	{
		await using var db = new DkpDbContext(new DbContextOptionsBuilder<DkpDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options);
		var officer = new User("officer", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		var target = new User("target", "Target", null, UserRole.Member, DateTime.UtcNow);
		db.Users.AddRange(officer, target);
		await db.SaveChangesAsync();
		var service = new DkpTransactionCommandService(
			new UserRepository(db),
			new DkpTransactionRepository(db));

		await service.AddAsync("officer", new CreateDkpTransactionRequest(target.Id, 30, "Raid"));
		await service.RemoveAsync("officer", new CreateDkpTransactionRequest(target.Id, 5, "Correction"));

		var history = await new DkpQueries(db).GetHistoryAsync("target");

		Assert.NotNull(history);
		Assert.Equal(25, history.Balance.Amount);
		Assert.Equal(2, history.Transactions.Count);
	}

	private sealed class FakeUserRepository(params User[] users)
		: IUserRepository
	{
		private readonly List<User> users = [.. users];

		public Task<User?> FindByDiscordIdAsync(string discordId, CancellationToken cancellationToken = default)
			=> Task.FromResult(users.SingleOrDefault(user => user.DiscordId == discordId));

		public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
			=> Task.FromResult(users.SingleOrDefault(user => user.Id == id));

		public Task AddAsync(User user, CancellationToken cancellationToken = default)
		{
			users.Add(user);
			return Task.CompletedTask;
		}

		public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}

	private sealed class FakeDkpTransactionRepository : IDkpTransactionRepository
	{
		public List<DkpTransaction> Transactions { get; } = [];

	public Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
		=> Task.FromResult(Transactions.Where(transaction => transaction.UserId == userId).Sum(transaction => transaction.Amount));

		public Task AddAsync(DkpTransaction transaction, CancellationToken cancellationToken = default)
		{
			Transactions.Add(transaction);
			return Task.CompletedTask;
		}

		public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
