using DKP.Domain;

namespace DKP.Application.Characters;

public interface ICharacterCommandService
{
	Task<Character> CreateAsync(string discordId, string firstName, string lastName, CancellationToken cancellationToken = default);
	Task<Character?> UpdateAsync(string discordId, Guid characterId, string firstName, string lastName, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default);
}
