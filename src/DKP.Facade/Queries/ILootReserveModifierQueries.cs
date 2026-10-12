using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface ILootReserveModifierQueries
{
    Task<IReadOnlyList<LootReserveModifierDto>> GetActiveAsync(CancellationToken cancellationToken = default);
}
