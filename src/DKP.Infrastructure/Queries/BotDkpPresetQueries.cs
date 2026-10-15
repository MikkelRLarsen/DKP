using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotDkpPresetQueries(IDbContextFactory<DkpDbContext> factory) : IBotDkpPresetQueries
{
    public async Task<IReadOnlyList<DkpAcquisitionSourceDto>?> GetAvailableSourcesAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        var state = LedgerReplayState.Replay(await db.DkpEvents.AsNoTracking().Where(x => x.UserId == user.Id).ToArrayAsync(ct));
        var presets = await db.DkpAwardPresets.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToArrayAsync(ct);
        var pendingByPreset = await db.DkpAwardRequests.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.Status == DKP.Domain.DkpAwardRequestStatus.Pending && x.PresetId != null)
            .GroupBy(x => x.PresetId!.Value)
            .Select(x => new { PresetId = x.Key, Quantity = x.Sum(r => r.Quantity) })
            .ToDictionaryAsync(x => x.PresetId, x => x.Quantity, ct);
        return presets.Select(x =>
            {
                var used = state.PresetUsage.GetValueOrDefault(x.Id);
                var pending = pendingByPreset.GetValueOrDefault(x.Id);
                var remaining = Math.Max(0, x.MaxApplicationsPerUser - used - pending);
                return new DkpAcquisitionSourceDto(x.Id, x.Name, x.Amount, x.Reason, used + pending, x.MaxApplicationsPerUser, remaining);
            })
            .Where(x => x.Remaining > 0)
            .ToArray();
    }
}
