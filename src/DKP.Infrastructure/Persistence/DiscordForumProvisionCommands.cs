using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DKP.Infrastructure.Persistence;

public sealed class DiscordForumProvisionCommands(IDbContextFactory<DkpDbContext> factory, ICurrentUser currentUser, IConfiguration configuration, TimeProvider time) : IDiscordForumProvisionCommands
{
    private static readonly string[] Keys = ["bulletin-board", "achievements", "dkp-sources", "shop", "bot-help"];
    public async Task<DiscordForumProvisionDto> RequestAsync(string sectionKey, CancellationToken cancellationToken = default)
    {
        await EnsureOfficerAsync(cancellationToken); var key = NormalizeKey(sectionKey); var guildId = RequiredGuildId();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.DiscordForumProvisionRequests.Where(x => x.GuildId == guildId && x.SectionKey == key && (x.Status == DiscordForumProvisionRequestStatus.Pending || x.Status == DiscordForumProvisionRequestStatus.Processing)).OrderByDescending(x => x.RequestedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return Map(existing);
        var request = new DiscordForumProvisionRequest(guildId, key, time.GetUtcNow().UtcDateTime); db.DiscordForumProvisionRequests.Add(request); await db.SaveChangesAsync(cancellationToken); return Map(request);
    }
    public async Task<DiscordForumProvisionDto> CompleteAsync(Guid requestId, ulong forumChannelId, ulong threadId, ulong messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken); var request = await db.DiscordForumProvisionRequests.SingleAsync(x => x.Id == requestId, cancellationToken); var now = time.GetUtcNow().UtcDateTime;
        var section = await db.DiscordForumSections.SingleOrDefaultAsync(x => x.GuildId == request.GuildId && x.SectionKey == request.SectionKey, cancellationToken);
        if (section is null) { section = new DiscordForumSection(request.GuildId, request.SectionKey, DiscordForumDefaults.Content(request.SectionKey), now); db.DiscordForumSections.Add(section); }
        section.Register(forumChannelId, threadId, messageId, now); request.Complete(threadId, messageId, now); await db.SaveChangesAsync(cancellationToken); return Map(request);
    }
    public async Task<DiscordForumProvisionDto> FailAsync(Guid requestId, string error, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken); var request = await db.DiscordForumProvisionRequests.SingleAsync(x => x.Id == requestId, cancellationToken); request.Fail(error, time.GetUtcNow().UtcDateTime); await db.SaveChangesAsync(cancellationToken); return Map(request);
    }
    private async Task EnsureOfficerAsync(CancellationToken ct)
    {
        var id = await currentUser.GetDiscordIdAsync(ct); if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Login is required.");
        await using var db = await factory.CreateDbContextAsync(ct); var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == id, ct); if (user is null || user.IsBlocked || user.Role != DKP.Domain.UserRole.Officer) throw new UnauthorizedAccessException("An active Officer is required.");
    }
    private string RequiredGuildId() => configuration["Discord:GuildId"]?.Trim() is { Length: > 0 } value ? value : throw new InvalidOperationException("Discord:GuildId is not configured.");
    private static string NormalizeKey(string value) => Keys.FirstOrDefault(x => x.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Unknown Discord forum section.");
    private static DiscordForumProvisionDto Map(DiscordForumProvisionRequest x) => new(x.Id, x.GuildId, x.SectionKey, (DiscordForumProvisionStatus)x.Status, x.RequestedAtUtc, x.ProcessedAtUtc, x.NewThreadId, x.NewMessageId, x.ErrorMessage);
}
