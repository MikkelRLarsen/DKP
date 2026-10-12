using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBotCharacterQueries
{
    Task<IReadOnlyList<CharacterDto>?> GetAsync(string discordId, CancellationToken cancellationToken = default);
}
