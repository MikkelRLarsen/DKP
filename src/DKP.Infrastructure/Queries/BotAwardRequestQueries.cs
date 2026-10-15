using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotAwardRequestQueries(IDbContextFactory<DkpDbContext> factory) : IBotAwardRequestQueries
{
    public async Task<IReadOnlyList<DkpAwardRequestDto>?> GetPendingAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (actor is null || actor.IsBlocked || actor.Role != DKP.Domain.UserRole.Officer) return null;

        var rows = await db.DkpAwardRequests.AsNoTracking()
            .Where(x => x.Status == DKP.Domain.DkpAwardRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(ct);
        if (rows.Length == 0) return [];

        var userIds = rows.Select(x => x.UserId).Append(actor.Id).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var presetIds = rows.Where(x => x.PresetId.HasValue).Select(x => x.PresetId!.Value).Distinct().ToArray();
        var presets = await db.DkpAwardPresets.AsNoTracking().Where(x => presetIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var achievementIds = rows.Where(x => x.AchievementId.HasValue).Select(x => x.AchievementId!.Value).Distinct().ToArray();
        var achievements = await db.AchievementDefinitions.AsNoTracking().Where(x => achievementIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

        return rows.Select(row =>
        {
            var user = users[row.UserId];
            var main = user.Characters.FirstOrDefault(x => x.IsMain);
            var preset = row.PresetId is Guid presetId ? presets.GetValueOrDefault(presetId) : null;
            var achievement = row.AchievementId is Guid achievementId ? achievements.GetValueOrDefault(achievementId) : null;
            return new DkpAwardRequestDto(row.Id, row.UserId, user.DiscordName,
                main is null ? null : $"{main.FirstName} {main.LastName}", row.PresetId, row.AchievementId,
                achievement?.Name ?? preset?.Name ?? "Unknown source", achievement?.DkpAmount ?? preset?.Amount ?? 0,
                row.Quantity, achievement?.Description ?? preset?.Reason ?? string.Empty, row.Comment,
                DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, row.CreatedAtUtc, null, null, null, row.DkpEventIds);
        }).ToArray();
    }
}
