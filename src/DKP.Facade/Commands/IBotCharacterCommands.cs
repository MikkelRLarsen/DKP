using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotCharacterCommands
{
    Task<CharacterDto> CreateAsync(string discordId, CharacterInput input, CancellationToken cancellationToken = default);
    Task<CharacterDto?> UpdateAsync(string discordId, Guid characterId, CharacterInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default);
    Task<bool> SetMainCharacterAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default);
}
