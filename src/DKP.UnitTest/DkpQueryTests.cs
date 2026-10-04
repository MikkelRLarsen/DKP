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

	[Fact]
	public async Task Guild_members_query_returns_all_members_with_balances_and_characters()
	{
		var officer = new User("officer", "Officer", "officer-avatar", UserRole.Officer, DateTime.UtcNow);
		var member = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		var mainCharacter = new Character(member.Id, "Zed", "Main");
		mainCharacter.SetAsMain();
		member.Characters.Add(mainCharacter);
		member.Characters.Add(new Character(member.Id, "Alpha", "Alt"));
		await using var db = CreateDatabase();
		db.Users.AddRange(officer, member);
		db.DkpTransactions.AddRange(
			new DkpTransaction(member.Id, 100, "Raid", officer.Id, DateTime.UtcNow.AddMinutes(-2)),
			new DkpTransaction(member.Id, -25, "Correction", officer.Id, DateTime.UtcNow.AddMinutes(-1)));
		await db.SaveChangesAsync();

		var result = await new GuildMemberQueries(db).GetAllAsync();

		Assert.Equal(2, result.Count);
		var loadedMember = Assert.Single(result, item => item.UserId == member.Id);
		Assert.Equal(75, loadedMember.DkpBalance);
		Assert.Equal(2, loadedMember.Characters.Count);
		Assert.Equal("Zed", loadedMember.Characters[0].FirstName);
		Assert.True(loadedMember.Characters[0].IsMain);
	}

	[Fact]
	public async Task Guild_members_query_includes_users_without_characters_with_zero_balance()
	{
		var user = new User("user-1", "No Character", null, UserRole.Member, DateTime.UtcNow);
		await using var db = CreateDatabase();
		db.Users.Add(user);
		await db.SaveChangesAsync();

		var result = await new GuildMemberQueries(db).GetAllAsync();

		var loadedUser = Assert.Single(result);
		Assert.Equal(user.Id, loadedUser.UserId);
		Assert.Equal(0, loadedUser.DkpBalance);
		Assert.Empty(loadedUser.Characters);
	}

	[Fact]
	public async Task User_summary_contains_discord_name_and_main_character_for_officer_selection()
	{
		var user = new User("user-1", "DiscordName", null, UserRole.Member, DateTime.UtcNow);
		var main = new Character(user.Id, "Main", "Character");
		main.SetAsMain();
		user.Characters.Add(main);
		await using var db = CreateDatabase();
		db.Users.Add(user);
		await db.SaveChangesAsync();

		var result = await new DkpQueries(db).GetUsersAsync();

		var summary = Assert.Single(result);
		Assert.Equal("Main Character", summary.MainCharacterName);
		Assert.Equal("DiscordName / Main Character", summary.DisplayName);
	}

	[Fact]
	public async Task Player_details_returns_profile_characters_balance_and_history()
	{
		var officer = new User("officer", "Officer", "officer-avatar", UserRole.Officer, DateTime.UtcNow);
		var member = new User("member", "Member", "member-avatar", UserRole.Member, DateTime.UtcNow);
		var mainCharacter = new Character(member.Id, "Main", "Character");
		mainCharacter.SetAsMain();
		member.Characters.Add(mainCharacter);
		member.Characters.Add(new Character(member.Id, "Alt", "Character"));
		await using var db = CreateDatabase();
		db.Users.AddRange(officer, member);
		db.DkpTransactions.Add(new DkpTransaction(member.Id, 40, "Raid", officer.Id, DateTime.UtcNow));
		await db.SaveChangesAsync();

		var result = await new PlayerDetailsQueries(db).GetAsync(member.Id);

		Assert.NotNull(result);
		Assert.Equal("Member", result.DiscordName);
		Assert.Equal("member-avatar", result.AvatarUrl);
		Assert.Equal(40, result.DkpHistory.Balance.Amount);
		Assert.Equal(2, result.Characters.Count);
		Assert.True(result.Characters[0].IsMain);
		var transaction = Assert.Single(result.DkpHistory.Transactions);
		Assert.Equal("Officer", transaction.CreatedByDiscordName);
	}

	[Fact]
	public async Task Player_details_returns_null_for_unknown_user()
	{
		await using var db = CreateDatabase();

		var result = await new PlayerDetailsQueries(db).GetAsync(Guid.NewGuid());

		Assert.Null(result);
	}

	private static DkpDbContext CreateDatabase()
	{
		return new DkpDbContext(new DbContextOptionsBuilder<DkpDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options);
	}
}
