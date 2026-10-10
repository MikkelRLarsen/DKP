using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotDkpRequestCommands
{
    Task<IReadOnlyList<DkpAwardRequestDto>> CreateAsync(string discordId, Guid presetId, int quantity, IReadOnlyList<string>? targetDiscordIds, string? comment, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(string discordId, Guid requestId, CancellationToken cancellationToken = default);
}
