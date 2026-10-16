using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IDiscordForumSectionCommands
{
    Task<DiscordForumSectionDto> UpdateContentAsync(string sectionKey, UpdateDiscordForumSectionRequest request, CancellationToken cancellationToken = default);
    Task<DiscordForumSectionDto> RegisterAsync(RegisterDiscordForumSectionRequest request, CancellationToken cancellationToken = default);
}
