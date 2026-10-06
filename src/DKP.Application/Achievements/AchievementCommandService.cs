using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Achievements;

public sealed class AchievementCommandService(CommandContext context, IAchievementRepository achievements, IEventLedgerRepository ledger, TimeProvider time) : IAchievementCommands
{
    private static void Validate(AchievementInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Key) || input.Key.Trim().Length > 64 || string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 128 || input.Description?.Trim().Length > 500 || input.DkpAmount <= 0)
            throw new ArgumentException("Key, name, description (max 500) and a positive DKP amount are required.");
    }
    private static AchievementDefinitionDto ToDto(AchievementDefinition x) => new(x.Id, x.Key, x.Name, x.Description, x.DkpAmount, x.IsActive);
    public Task<AchievementDefinitionDto> CreateAsync(AchievementInput input, CancellationToken ct = default) => context.ExecuteAsync([], true, async _ => { Validate(input); var item = new AchievementDefinition(input.Key.Trim(), input.Name.Trim(), input.Description.Trim(), input.DkpAmount, time.GetUtcNow().UtcDateTime); await achievements.AddAsync(item, ct); return ToDto(item); }, ct);
    public Task<AchievementDefinitionDto> UpdateAsync(Guid id, AchievementInput input, CancellationToken ct = default) => context.ExecuteAsync([], true, async _ => { Validate(input); var item = await achievements.FindAsync(id, ct) ?? throw new KeyNotFoundException("Achievement not found."); item.Update(input.Key.Trim(), input.Name.Trim(), input.Description.Trim(), input.DkpAmount, time.GetUtcNow().UtcDateTime); return ToDto(item); }, ct);
    public Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default) => context.ExecuteAsync([], true, async _ => { var item = await achievements.FindAsync(id, ct) ?? throw new KeyNotFoundException("Achievement not found."); item.SetActive(active, time.GetUtcNow().UtcDateTime); return true; }, ct);
    public Task<IReadOnlyList<UserAchievementDto>> GrantAsync(Guid achievementId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        var ids = CommandContext.Targets(userIds);
        return context.ExecuteAsync<IReadOnlyList<UserAchievementDto>>(ids, true, async officer =>
        {
            var definition = await achievements.FindAsync(achievementId, ct) ?? throw new KeyNotFoundException("Achievement not found.");
            if (!definition.IsActive) throw new InvalidOperationException("Achievement is inactive.");
            var targets = new List<User>();
            foreach (var id in ids)
            {
                var target = await context.TargetAsync(id, ct);
                if (await achievements.HasActiveAsync(id, achievementId, ct)) throw new InvalidOperationException($"{target.DiscordName} already has this achievement.");
                targets.Add(target);
            }
            var now = time.GetUtcNow().UtcDateTime;
            var operation = Guid.NewGuid();
            var result = new List<UserAchievementDto>();
            foreach (var target in targets)
            {
                var entry = await ledger.PostAsync(target.Id, officer.Id, operation, now, new DkpPosted(definition.DkpAmount, definition.Name, null, achievementId), ct);
                var grant = new UserAchievement(target.Id, achievementId, officer.Id, entry.Id, now);
                await achievements.AddUserAchievementAsync(grant, ct);
                result.Add(new(grant.Id, target.Id, target.DiscordName, achievementId, definition.Name, definition.DkpAmount, true, now, null));
            }
            return result;
        }, ct);
    }
    public Task RevokeAsync(Guid userAchievementId, CancellationToken ct = default) => context.ExecuteAsync([], true, async officer =>
    {
        var grant = await achievements.FindUserAchievementAsync(userAchievementId, ct) ?? throw new KeyNotFoundException("User achievement not found.");
        if (grant.RevokedAtUtc is not null) throw new InvalidOperationException("Achievement is already revoked.");
        var definition = await achievements.FindAsync(grant.AchievementId, ct) ?? throw new KeyNotFoundException("Achievement definition not found.");
        var now = time.GetUtcNow().UtcDateTime;
        var entry = await ledger.PostAsync(grant.UserId, officer.Id, Guid.NewGuid(), now, new DkpPosted(-definition.DkpAmount, $"Revoke achievement: {definition.Name}", null, definition.Id), ct);
        grant.Revoke(officer.Id, entry.Id, now);
        return true;
    }, ct);
}
