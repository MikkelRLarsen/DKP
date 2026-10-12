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
        return presets.Where(x => state.PresetUsage.GetValueOrDefault(x.Id) < x.MaxApplicationsPerUser)
            .Select(x => { var used = state.PresetUsage.GetValueOrDefault(x.Id); return new DkpAcquisitionSourceDto(x.Id, x.Name, x.Amount, x.Reason, used, x.MaxApplicationsPerUser, x.MaxApplicationsPerUser - used); })
            .ToArray();
    }
}
