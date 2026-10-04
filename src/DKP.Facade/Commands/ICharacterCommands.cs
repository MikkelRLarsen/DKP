using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface ICharacterCommands
{
	Task<CharacterDto> CreateAsync(string authenticatedDiscordId, CharacterInput input, CancellationToken cancellationToken = default);
	Task<CharacterDto?> UpdateAsync(string authenticatedDiscordId, Guid characterId, CharacterInput input, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(string authenticatedDiscordId, Guid characterId, CancellationToken cancellationToken = default);
	Task<bool> SetMainCharacterAsync(string authenticatedDiscordId, Guid characterId, CancellationToken cancellationToken = default);
}
