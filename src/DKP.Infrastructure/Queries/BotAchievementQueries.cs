using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotAchievementQueries(IDbContextFactory<DkpDbContext> factory) : IBotAchievementQueries
{
    public async Task<BotAchievementOverviewDto?> GetOverviewAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().Include(x => x.Characters).SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;

        var definitions = await db.AchievementDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new AchievementDefinitionDto(x.Id, x.Key, x.Name, x.Description, x.DkpAmount, x.IsActive)).ToArrayAsync(ct);
        var userAchievements = await db.UserAchievements.AsNoTracking().Where(x => x.UserId == user.Id).OrderByDescending(x => x.GrantedAtUtc).ToArrayAsync(ct);
        var definitionMap = await db.AchievementDefinitions.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        var achievementDtos = userAchievements.Where(x => definitionMap.ContainsKey(x.AchievementId)).Select(x =>
        {
            var definition = definitionMap[x.AchievementId];
            return new UserAchievementDto(x.Id, user.Id, user.DiscordName, x.AchievementId, definition.Name, definition.DkpAmount, x.RevokedAtUtc is null, x.GrantedAtUtc, x.RevokedAtUtc);
        }).ToArray();

        var rows = await db.DkpAwardRequests.AsNoTracking().Where(x => x.UserId == user.Id).OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(ct);
        var presetIds = rows.Where(x => x.PresetId is not null).Select(x => x.PresetId!.Value).Distinct().ToArray();
        var presets = await db.DkpAwardPresets.AsNoTracking().Where(x => presetIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var requestDtos = rows.Select(row =>
        {
            var achievement = row.AchievementId is Guid achievementId && definitionMap.TryGetValue(achievementId, out var definition) ? definition : null;
            var preset = row.PresetId is Guid presetId && presets.TryGetValue(presetId, out var presetValue) ? presetValue : null;
            var main = user.Characters.FirstOrDefault(x => x.IsMain);
            return new DkpAwardRequestDto(row.Id, row.UserId, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", row.PresetId, row.AchievementId,
                achievement?.Name ?? preset?.Name ?? "Unknown source", achievement?.DkpAmount ?? preset?.Amount ?? 0, row.Quantity,
                achievement?.Description ?? preset?.Reason ?? string.Empty, row.Comment, ToStatus(row.Status), row.CreatedAtUtc, row.ReviewedAtUtc, null, row.ReviewComment, row.DkpEventIds);
        }).ToArray();
        return new BotAchievementOverviewDto(definitions, achievementDtos, requestDtos);
    }

    private static DkpAwardRequestStatus ToStatus(DKP.Domain.DkpAwardRequestStatus status) => status switch
    {
        DKP.Domain.DkpAwardRequestStatus.Pending => DkpAwardRequestStatus.Pending,
        DKP.Domain.DkpAwardRequestStatus.Approved => DkpAwardRequestStatus.Approved,
        DKP.Domain.DkpAwardRequestStatus.Rejected => DkpAwardRequestStatus.Rejected,
        DKP.Domain.DkpAwardRequestStatus.Cancelled => DkpAwardRequestStatus.Cancelled,
        _ => throw new InvalidOperationException("Unknown request status.")
    };
}
