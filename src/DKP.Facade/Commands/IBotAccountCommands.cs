using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotAccountCommands
{
    Task<BotAccountDto> CreateAsync(string discordId, BotAccountInput input, CancellationToken cancellationToken = default);
}
