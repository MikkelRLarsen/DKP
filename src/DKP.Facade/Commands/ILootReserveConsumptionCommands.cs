using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface ILootReserveConsumptionCommands
{
    Task<LootReserveConsumptionResultDto> PreviewConsumeAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
    Task<LootReserveConsumptionResultDto> ConsumeAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
    Task<LootReserveConsumptionBatchDto> RevertLatestAsync(CancellationToken cancellationToken = default);
}
