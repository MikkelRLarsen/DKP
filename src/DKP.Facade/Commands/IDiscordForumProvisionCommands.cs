using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IDiscordForumProvisionCommands
{
    Task<DiscordForumProvisionDto> RequestAsync(string sectionKey, CancellationToken cancellationToken = default);
    Task<DiscordForumProvisionDto> CompleteAsync(Guid requestId, ulong forumChannelId, ulong threadId, ulong messageId, CancellationToken cancellationToken = default);
    Task<DiscordForumProvisionDto> FailAsync(Guid requestId, string error, CancellationToken cancellationToken = default);
}
