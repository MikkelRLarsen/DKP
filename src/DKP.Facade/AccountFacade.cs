using DKP.Application.Characters;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;

namespace DKP.Facade;

public sealed class AccountFacade(
	IAccountQueries accountQueries,
	ICharacterCommandService characterCommands) : IAccountFacade
{
	public Task<DashboardDto?> GetDashboardAsync(string discordId, CancellationToken cancellationToken = default)
		=> accountQueries.GetDashboardAsync(discordId, cancellationToken);

	public async Task<CharacterDto> CreateCharacterAsync(string discordId, CharacterInput input, CancellationToken cancellationToken = default)
	{
		var character = await characterCommands.CreateAsync(discordId, input.FirstName, input.LastName, cancellationToken);
		return new CharacterDto(character.Id, character.FirstName, character.LastName);
	}

	public async Task<CharacterDto?> UpdateCharacterAsync(string discordId, Guid characterId, CharacterInput input, CancellationToken cancellationToken = default)
	{
		var character = await characterCommands.UpdateAsync(discordId, characterId, input.FirstName, input.LastName, cancellationToken);
		return character is null ? null : new CharacterDto(character.Id, character.FirstName, character.LastName);
	}

	public Task<bool> DeleteCharacterAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default)
		=> characterCommands.DeleteAsync(discordId, characterId, cancellationToken);
}
