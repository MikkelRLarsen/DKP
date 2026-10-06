using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface ILootReserveConsumptionQueries
{
    Task<LootReserveConsumptionBatchDto?> GetLatestConsumptionBatchAsync(CancellationToken cancellationToken = default);
}
