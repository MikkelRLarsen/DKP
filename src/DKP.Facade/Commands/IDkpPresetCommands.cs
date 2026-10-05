using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IDkpPresetCommands
{
	Task<DkpAwardPresetDto> CreateAsync(string actorDiscordId, DkpAwardPresetInput input, CancellationToken cancellationToken = default);
	Task<DkpAwardPresetDto> UpdateAsync(string actorDiscordId, Guid id, DkpAwardPresetInput input, CancellationToken cancellationToken = default);
	Task SetActiveAsync(string actorDiscordId, Guid id, bool active, CancellationToken cancellationToken = default);
	Task<DkpTransactionDto> ApplyAsync(string actorDiscordId, Guid targetUserId, Guid presetId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> ApplyManyAsync(string actorDiscordId, Guid presetId, IReadOnlyCollection<Guid> targetUserIds, CancellationToken cancellationToken = default);
}
