using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DKP.Infrastructure.Queries;

public sealed class DiscordForumProvisionQueries(IDbContextFactory<DkpDbContext> factory, QuerySession session, IConfiguration configuration) : IDiscordForumProvisionQueries
{
    public async Task<IReadOnlyList<DiscordForumProvisionDto>> GetPendingForBotAsync(CancellationToken cancellationToken = default)
    {
        var guildId = RequiredGuildId();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var staleProcessingBefore = DateTime.UtcNow.AddMinutes(-2);
        var candidates = await db.DiscordForumProvisionRequests.Where(x => x.GuildId == guildId && (x.Status == DiscordForumProvisionRequestStatus.Pending || (x.Status == DiscordForumProvisionRequestStatus.Processing && x.RequestedAtUtc < staleProcessingBefore))).OrderBy(x => x.RequestedAtUtc).Take(10).Select(x => x.Id).ToArrayAsync(cancellationToken);
        var claimed = new List<Guid>();
        foreach (var id in candidates)
        {
            var affected = await db.DiscordForumProvisionRequests.Where(x => x.Id == id && (x.Status == DiscordForumProvisionRequestStatus.Pending || (x.Status == DiscordForumProvisionRequestStatus.Processing && x.RequestedAtUtc < staleProcessingBefore))).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, DiscordForumProvisionRequestStatus.Processing), cancellationToken);
            if (affected == 1) claimed.Add(id);
        }
        if (claimed.Count == 0) return [];
        var requests = await db.DiscordForumProvisionRequests.AsNoTracking().Where(x => claimed.Contains(x.Id)).ToArrayAsync(cancellationToken);
        return requests.OrderBy(x => x.RequestedAtUtc).Select(Map).ToArray();
    }
    public Task<DiscordForumProvisionDto?> GetLatestAsync(string guildId, string sectionKey, CancellationToken cancellationToken = default) => session.ReadAsync<DiscordForumProvisionDto?>(true, async (db, _) => { var request = await db.DiscordForumProvisionRequests.AsNoTracking().Where(x => x.GuildId == guildId && x.SectionKey == sectionKey).OrderByDescending(x => x.RequestedAtUtc).FirstOrDefaultAsync(cancellationToken); return request is null ? null : Map(request); }, cancellationToken);
    private string RequiredGuildId() => configuration["Discord:GuildId"]?.Trim() is { Length: > 0 } value ? value : throw new InvalidOperationException("Discord:GuildId is not configured.");
    private static DiscordForumProvisionDto Map(DiscordForumProvisionRequest x) => new(x.Id, x.GuildId, x.SectionKey, (DiscordForumProvisionStatus)x.Status, x.RequestedAtUtc, x.ProcessedAtUtc, x.NewThreadId, x.NewMessageId, x.ErrorMessage);
}
