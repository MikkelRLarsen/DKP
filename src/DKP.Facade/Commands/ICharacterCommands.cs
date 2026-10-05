using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface ICharacterCommands
{
	Task<CharacterDto> CreateAsync(CharacterInput input, CancellationToken cancellationToken = default);
	Task<CharacterDto?> UpdateAsync(Guid characterId, CharacterInput input, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(Guid characterId, CancellationToken cancellationToken = default);
	Task<bool> SetMainCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);
}
