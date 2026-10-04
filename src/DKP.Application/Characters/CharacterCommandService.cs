using DKP.Application.Persistence;
using DKP.Domain;

namespace DKP.Application.Characters;

public sealed class CharacterCommandService(
	IUserRepository users,
	ICharacterRepository characters) : ICharacterCommandService
{
	public async Task<Character> CreateAsync(string discordId, string firstName, string lastName, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		Validate(firstName, lastName);
		var character = new Character(user.Id, firstName.Trim(), lastName.Trim());
		await characters.AddAsync(character, cancellationToken);
		await characters.SaveChangesAsync(cancellationToken);
		return character;
	}

	public async Task<Character?> UpdateAsync(string discordId, Guid characterId, string firstName, string lastName, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		Validate(firstName, lastName);
		var character = await characters.FindForUserAsync(characterId, user.Id, cancellationToken);
		if (character is null)
		{
			return null;
		}

		character.Update(firstName.Trim(), lastName.Trim());
		await characters.SaveChangesAsync(cancellationToken);
		return character;
	}

	public async Task<bool> DeleteAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		var character = await characters.FindForUserAsync(characterId, user.Id, cancellationToken);
		if (character is null)
		{
			return false;
		}

		characters.Remove(character);
		await characters.SaveChangesAsync(cancellationToken);
		return true;
	}

	private async Task<User> GetUserAsync(string discordId, CancellationToken cancellationToken)
		=> await users.FindByDiscordIdAsync(discordId, cancellationToken)
			?? throw new InvalidOperationException("The authenticated Discord user does not exist.");

	private static void Validate(string firstName, string lastName)
	{
		if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 64)
		{
			throw new ArgumentException("First name is required and must be at most 64 characters.", nameof(firstName));
		}

		if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 64)
		{
			throw new ArgumentException("Last name is required and must be at most 64 characters.", nameof(lastName));
		}
	}
}
