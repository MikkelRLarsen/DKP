using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
using DKP.Infrastructure.Persistence;

namespace DKP.Infrastructure.Queries;

public sealed class DiscordForumSectionQueries(QuerySession session, IDbContextFactory<DkpDbContext> factory) : IDiscordForumSectionQueries
{
    public Task<IReadOnlyList<DiscordForumSectionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => session.ReadAsync<IReadOnlyList<DiscordForumSectionDto>>(true, async (db, _) => await db.DiscordForumSections.AsNoTracking()
            .OrderBy(x => x.SectionKey)
            .Select(x => new DiscordForumSectionDto(x.Id, x.GuildId, x.SectionKey, x.ForumChannelId, x.ThreadId, x.InitialMessageId, x.InitialMessageContent, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<DiscordForumSectionDto>> GetAllForBotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.DiscordForumSections.AsNoTracking().OrderBy(x => x.SectionKey)
            .Select(x => new DiscordForumSectionDto(x.Id, x.GuildId, x.SectionKey, x.ForumChannelId, x.ThreadId, x.InitialMessageId, x.InitialMessageContent, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }
}
