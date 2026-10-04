using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Domain;

namespace DKP.Application.Characters;

public sealed class CharacterCommandService(
	IUserRepository users,
	ICharacterRepository characters) : ICharacterCommands
{
	public async Task<CharacterDto> CreateAsync(string discordId, CharacterInput input, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		Validate(input.FirstName, input.LastName);
		var character = new Character(user.Id, input.FirstName.Trim(), input.LastName.Trim());
		await characters.AddAsync(character, cancellationToken);
		await characters.SaveChangesAsync(cancellationToken);
		return ToDto(character);
	}

	public async Task<CharacterDto?> UpdateAsync(string discordId, Guid characterId, CharacterInput input, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		Validate(input.FirstName, input.LastName);
		var character = await characters.FindForUserAsync(characterId, user.Id, cancellationToken);
		if (character is null)
		{
			return null;
		}

		character.Update(input.FirstName.Trim(), input.LastName.Trim());
		await characters.SaveChangesAsync(cancellationToken);
		return ToDto(character);
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

	public async Task<bool> SetMainCharacterAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default)
	{
		var user = await GetUserAsync(discordId, cancellationToken);
		var userCharacters = await characters.FindAllForUserAsync(user.Id, cancellationToken);
		var selected = userCharacters.SingleOrDefault(character => character.Id == characterId);
		if (selected is null)
		{
			return false;
		}

		if (selected.IsMain)
		{
			return true;
		}

		var previousMain = false;
		foreach (var character in userCharacters)
		{
			if (character.IsMain)
			{
				character.ClearMain();
				previousMain = true;
			}
		}

		if (previousMain)
		{
			await characters.SaveChangesAsync(cancellationToken);
		}

		selected.SetAsMain();
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

	private static CharacterDto ToDto(Character character)
		=> new(character.Id, character.FirstName, character.LastName, character.IsMain);
}
