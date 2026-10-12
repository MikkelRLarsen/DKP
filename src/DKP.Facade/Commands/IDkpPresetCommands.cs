using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IDkpPresetCommands
{
	Task<DkpAwardPresetDto> CreateAsync(DkpAwardPresetInput input, CancellationToken cancellationToken = default);
	Task<DkpAwardPresetDto> UpdateAsync(Guid id, DkpAwardPresetInput input, CancellationToken cancellationToken = default);
	Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
	Task<DkpTransactionDto> ApplyAsync(Guid targetUserId, Guid presetId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> ApplyManyAsync(Guid presetId, IReadOnlyCollection<Guid> targetUserIds, CancellationToken cancellationToken = default);
}
