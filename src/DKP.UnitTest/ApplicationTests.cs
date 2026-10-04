using DKP.Application.Authentication;
using DKP.Application.Characters;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Infrastructure.Persistence;
using DKP.Facade.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DKP.UnitTest;

public sealed class ApplicationTests
{
	[Fact]
	public async Task Provisioning_creates_new_user_and_assigns_officer_role()
	{
		var users = new FakeUserRepository();
		var service = new UserProvisioningService(users, new FakeOfficerPolicy(true), TimeProvider.System);

		var user = await service.ProvisionAsync(new DiscordUserProfile("123", "Shock", "avatar"));

		Assert.Equal("123", user.DiscordId);
		Assert.Equal(UserRole.Officer, user.Role);
		Assert.Single(users.Users);
	}

	[Fact]
	public async Task Provisioning_updates_existing_user_without_creating_duplicate()
	{
		var users = new FakeUserRepository();
		var service = new UserProvisioningService(users, new FakeOfficerPolicy(false), TimeProvider.System);

		var first = await service.ProvisionAsync(new DiscordUserProfile("123", "OldName", null));
		var second = await service.ProvisionAsync(new DiscordUserProfile("123", "NewName", "avatar"));

		Assert.Equal(first.Id, second.Id);
		Assert.Equal("NewName", second.DiscordName);
		Assert.Equal(UserRole.Member, second.Role);
		Assert.Single(users.Users);
	}

	[Fact]
	public async Task Character_service_allows_multiple_characters_for_one_user()
	{
		var users = new FakeUserRepository();
		var user = new User("123", "Shock", null, UserRole.Member, DateTime.UtcNow);
		users.Users.Add(user);
		var characters = new FakeCharacterRepository();
		var service = new CharacterCommandService(users, characters);

		await service.CreateAsync("123", new CharacterInput("Shock", "Adin"));
		await service.CreateAsync("123", new CharacterInput("Holy", "Shock"));

		Assert.Equal(2, characters.Characters.Count);
		Assert.All(characters.Characters, character => Assert.Equal(user.Id, character.UserId));
	}

	[Fact]
	public async Task Character_service_cannot_update_another_users_character()
	{
		var users = new FakeUserRepository();
		var owner = new User("owner", "Owner", null, UserRole.Member, DateTime.UtcNow);
		var other = new User("other", "Other", null, UserRole.Member, DateTime.UtcNow);
		users.Users.Add(owner);
		users.Users.Add(other);
		var characters = new FakeCharacterRepository();
		var character = new Character(owner.Id, "Shock", "Adin");
		characters.Characters.Add(character);
		var service = new CharacterCommandService(users, characters);

		var result = await service.UpdateAsync("other", character.Id, new CharacterInput("Hacked", "Name"));

		Assert.Null(result);
		Assert.Equal("Shock", character.FirstName);
	}

	[Fact]
	public async Task Ef_mapping_persists_user_and_multiple_characters()
	{
		await using var db = new DkpDbContext(new DbContextOptionsBuilder<DkpDbContext>()
			.UseInMemoryDatabase(nameof(Ef_mapping_persists_user_and_multiple_characters))
			.Options);
		var user = new User("123", "Shock", null, UserRole.Member, DateTime.UtcNow);
		user.Characters.Add(new Character(user.Id, "Shock", "Adin"));
		user.Characters.Add(new Character(user.Id, "Holy", "Shock"));
		db.Users.Add(user);
		await db.SaveChangesAsync();

		var loaded = await db.Users.Include(item => item.Characters).SingleAsync();

		Assert.Equal(2, loaded.Characters.Count);
	}

	private sealed class FakeOfficerPolicy(bool officer) : IOfficerIdentityPolicy
	{
		public bool IsOfficer(string discordId) => officer;
	}

	private sealed class FakeUserRepository : IUserRepository
	{
		public List<User> Users { get; } = [];

		public Task<User?> FindByDiscordIdAsync(string discordId, CancellationToken cancellationToken = default)
			=> Task.FromResult(Users.SingleOrDefault(user => user.DiscordId == discordId));

		public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
			=> Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

		public Task AddAsync(User user, CancellationToken cancellationToken = default)
		{
			Users.Add(user);
			return Task.CompletedTask;
		}

		public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}

	private sealed class FakeCharacterRepository : ICharacterRepository
	{
		public List<Character> Characters { get; } = [];

		public Task<Character?> FindForUserAsync(Guid characterId, Guid userId, CancellationToken cancellationToken = default)
			=> Task.FromResult(Characters.SingleOrDefault(character => character.Id == characterId && character.UserId == userId));

		public Task AddAsync(Character character, CancellationToken cancellationToken = default)
		{
			Characters.Add(character);
			return Task.CompletedTask;
		}

		public void Remove(Character character) => Characters.Remove(character);

		public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
