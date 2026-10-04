using DKP.Domain;

namespace DKP.Application.Persistence;

public interface ICharacterRepository
{
	Task<Character?> FindForUserAsync(Guid characterId, Guid userId, CancellationToken cancellationToken = default);
	Task AddAsync(Character character, CancellationToken cancellationToken = default);
	void Remove(Character character);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
