using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class CharacterRepository(CommandUnitOfWork session) : ICharacterRepository
{
	public Task<Character?> FindForUserAsync(Guid characterId, Guid userId, CancellationToken cancellationToken = default)
		=> session.Db.Characters.SingleOrDefaultAsync(character => character.Id == characterId && character.UserId == userId, cancellationToken);

	public async Task<IReadOnlyList<Character>> FindAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
		=> await session.Db.Characters.Where(character => character.UserId == userId).ToListAsync(cancellationToken);

	public Task AddAsync(Character character, CancellationToken cancellationToken = default)
	{
		session.Db.Characters.Add(character);
		return Task.CompletedTask;
	}

	public void Remove(Character character) => session.Db.Characters.Remove(character);

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> session.Db.SaveChangesAsync(cancellationToken);
}
