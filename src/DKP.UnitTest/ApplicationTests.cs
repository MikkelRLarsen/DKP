using DKP.Application.Authentication;
using DKP.Application.Characters;
using DKP.Application.Persistence;
using DKP.Application.Users;
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
		var service = new UserProvisioningService(users, new FakeOfficerPolicy("123"), TimeProvider.System);

		var user = await service.ProvisionAsync(new DiscordUserProfile("123", "Shock", "avatar"));

		Assert.Equal("123", user.DiscordId);
		Assert.Equal(UserRole.Officer, user.Role);
		Assert.Single(users.Users);
	}

	[Fact]
	public async Task Provisioning_updates_existing_user_without_creating_duplicate()
	{
		var users = new FakeUserRepository();
		var service = new UserProvisioningService(users, new FakeOfficerPolicy(), TimeProvider.System);

		var first = await service.ProvisionAsync(new DiscordUserProfile("123", "OldName", null));
		var second = await service.ProvisionAsync(new DiscordUserProfile("123", "NewName", "avatar"));

		Assert.Equal(first.Id, second.Id);
		Assert.Equal("NewName", second.DiscordName);
		Assert.Equal(UserRole.Member, second.Role);
		Assert.Single(users.Users);
	}

	[Fact]
	public async Task Provisioning_preserves_role_changes_for_non_bootstrap_users()
	{
		var users = new FakeUserRepository();
		var user = new User("123", "Member", null, UserRole.Officer, DateTime.UtcNow);
		users.Users.Add(user);
		var service = new UserProvisioningService(users, new FakeOfficerPolicy(), TimeProvider.System);

		var result = await service.ProvisionAsync(new DiscordUserProfile("123", "Member", null));

		Assert.Equal(UserRole.Officer, result.Role);
	}

	[Fact]
	public async Task Officer_can_change_member_role()
	{
		var users = new FakeUserRepository();
		var officer = new User("officer", "Officer", null, UserRole.Officer, DateTime.UtcNow);
		var member = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		users.Users.AddRange([officer, member]);
		var service = new UserRoleCommandService(users, new FakeOfficerPolicy("officer"));

		var result = await service.SetRoleAsync("officer", new SetUserRoleRequest(member.Id, UserRole.Officer));

		Assert.True(result);
		Assert.Equal(UserRole.Officer, member.Role);
	}

	[Fact]
	public async Task Member_cannot_change_user_roles()
	{
		var users = new FakeUserRepository();
		var member = new User("member", "Member", null, UserRole.Member, DateTime.UtcNow);
		var other = new User("other", "Other", null, UserRole.Member, DateTime.UtcNow);
		users.Users.AddRange([member, other]);
		var service = new UserRoleCommandService(users, new FakeOfficerPolicy());

		await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
			service.SetRoleAsync("member", new SetUserRoleRequest(other.Id, UserRole.Officer)));
	}

	[Fact]
	public async Task Bootstrap_officer_cannot_be_demoted()
	{
		var users = new FakeUserRepository();
		var officer = new User("bootstrap", "Bootstrap", null, UserRole.Officer, DateTime.UtcNow);
		var actor = new User("actor", "Actor", null, UserRole.Officer, DateTime.UtcNow);
		users.Users.AddRange([officer, actor]);
		var service = new UserRoleCommandService(users, new FakeOfficerPolicy("bootstrap", "actor"));

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			service.SetRoleAsync("actor", new SetUserRoleRequest(officer.Id, UserRole.Member)));
		Assert.Equal(UserRole.Officer, officer.Role);
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
	public async Task Character_service_sets_one_main_character_and_clears_previous_main()
	{
		var users = new FakeUserRepository();
		var owner = new User("owner", "Owner", null, UserRole.Member, DateTime.UtcNow);
		users.Users.Add(owner);
		var characters = new FakeCharacterRepository();
		var first = new Character(owner.Id, "First", "Character");
		var second = new Character(owner.Id, "Second", "Character");
		first.SetAsMain();
		characters.Characters.AddRange([first, second]);
		var service = new CharacterCommandService(users, characters);

		var result = await service.SetMainCharacterAsync("owner", second.Id);

		Assert.True(result);
		Assert.False(first.IsMain);
		Assert.True(second.IsMain);
	}

	[Fact]
	public async Task Character_service_cannot_set_another_users_character_as_main()
	{
		var users = new FakeUserRepository();
		var owner = new User("owner", "Owner", null, UserRole.Member, DateTime.UtcNow);
		var other = new User("other", "Other", null, UserRole.Member, DateTime.UtcNow);
		users.Users.AddRange([owner, other]);
		var characters = new FakeCharacterRepository();
		var character = new Character(other.Id, "Other", "Character");
		characters.Characters.Add(character);
		var service = new CharacterCommandService(users, characters);

		var result = await service.SetMainCharacterAsync("owner", character.Id);

		Assert.False(result);
		Assert.False(character.IsMain);
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

	private sealed class FakeOfficerPolicy(params string[] officerIds) : IOfficerIdentityPolicy
	{
		private readonly HashSet<string> ids = officerIds.ToHashSet(StringComparer.Ordinal);

		public bool IsOfficer(string discordId) => ids.Contains(discordId);
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

		public Task<IReadOnlyList<Character>> FindAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
			=> Task.FromResult<IReadOnlyList<Character>>(Characters.Where(character => character.UserId == userId).ToArray());

		public Task AddAsync(Character character, CancellationToken cancellationToken = default)
		{
			Characters.Add(character);
			return Task.CompletedTask;
		}

		public void Remove(Character character) => Characters.Remove(character);

		public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
