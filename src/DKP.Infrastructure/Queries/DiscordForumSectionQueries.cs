using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
using DKP.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace DKP.Infrastructure.Queries;

public sealed class DiscordForumSectionQueries(QuerySession session, IDbContextFactory<DkpDbContext> factory, IConfiguration configuration) : IDiscordForumSectionQueries
{
    private static readonly string[] Keys = ["bulletin-board", "achievements", "dkp-sources", "shop", "bot-help"];

    public Task<IReadOnlyList<DiscordForumSectionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => session.ReadAsync<IReadOnlyList<DiscordForumSectionDto>>(true, async (db, _) =>
        {
            var guildId = RequiredGuildId();
            var rows = await db.DiscordForumSections.AsNoTracking().Where(x => x.GuildId == guildId).ToArrayAsync(cancellationToken);
            return Keys.Select(key => rows.FirstOrDefault(x => x.SectionKey.Equals(key, StringComparison.OrdinalIgnoreCase)) is { } x
                    ? Map(x)
                    : new DiscordForumSectionDto(Guid.Empty, guildId, key, null, null, null, DiscordForumDefaults.Content(key), true, DateTime.MinValue, DateTime.MinValue))
                .ToArray();
        }, cancellationToken);

    public async Task<IReadOnlyList<DiscordForumSectionDto>> GetAllForBotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var guildId = RequiredGuildId();
        var rows = await db.DiscordForumSections.AsNoTracking().Where(x => x.GuildId == guildId).ToArrayAsync(cancellationToken);
        return Keys.Select(key => rows.FirstOrDefault(x => x.SectionKey.Equals(key, StringComparison.OrdinalIgnoreCase)) is { } x
                ? Map(x)
                : new DiscordForumSectionDto(Guid.Empty, guildId, key, null, null, null, DiscordForumDefaults.Content(key), true, DateTime.MinValue, DateTime.MinValue))
            .ToArray();
    }

    private string RequiredGuildId() => configuration["Discord:GuildId"]?.Trim() is { Length: > 0 } value ? value : throw new InvalidOperationException("Discord:GuildId is not configured.");
    private static DiscordForumSectionDto Map(DKP.Domain.DiscordForumSection x) => new(x.Id, x.GuildId, x.SectionKey, x.ForumChannelId, x.ThreadId, x.InitialMessageId, x.InitialMessageContent, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}
