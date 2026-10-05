using System.Text.Json;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Presets;

public sealed class DkpPresetCommandService(IUserRepository users, IPresetRepository presets, IEventLedgerRepository ledger, TimeProvider time) : IDkpPresetCommands
{
    private async Task<User> Actor(string id, CancellationToken ct) =>
        await users.FindByDiscordIdAsync(id, ct) ?? throw new UnauthorizedAccessException("Authenticated user does not exist.");

    private static void Officer(User user)
    {
        if (user.Role != UserRole.Officer || user.IsBlocked)
            throw new UnauthorizedAccessException("Only active Officers can manage presets.");
    }

    private static void Valid(DkpAwardPresetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Reason) ||
            input.Reason.Trim().Length > 500 || input.Amount <= 0 || input.MaxApplicationsPerUser <= 0)
            throw new ArgumentException("Invalid preset.");
    }

    public async Task<DkpAwardPresetDto> CreateAsync(string id, DkpAwardPresetInput input, CancellationToken ct = default)
    {
        var actor = await Actor(id, ct);
        Officer(actor);
        Valid(input);
        var preset = new DkpAwardPreset(input.Name.Trim(), input.Amount, input.Reason.Trim(), input.MaxApplicationsPerUser, time.GetUtcNow().UtcDateTime);
        await presets.AddAsync(preset, ct);
        await presets.SaveChangesAsync(ct);
        return new(preset.Id, preset.Name, preset.Amount, preset.Reason, preset.MaxApplicationsPerUser, preset.IsActive);
    }

    public async Task<DkpAwardPresetDto> UpdateAsync(string id, Guid presetId, DkpAwardPresetInput input, CancellationToken ct = default)
    {
        var actor = await Actor(id, ct);
        Officer(actor);
        Valid(input);
        var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
        preset.Update(input.Name.Trim(), input.Amount, input.Reason.Trim(), input.MaxApplicationsPerUser, time.GetUtcNow().UtcDateTime);
        await presets.SaveChangesAsync(ct);
        return new(preset.Id, preset.Name, preset.Amount, preset.Reason, preset.MaxApplicationsPerUser, preset.IsActive);
    }

    public async Task SetActiveAsync(string id, Guid presetId, bool active, CancellationToken ct = default)
    {
        var actor = await Actor(id, ct);
        Officer(actor);
        var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
        preset.SetActive(active, time.GetUtcNow().UtcDateTime);
        await presets.SaveChangesAsync(ct);
    }

    public async Task<DkpTransactionDto> ApplyAsync(string id, Guid targetId, Guid presetId, CancellationToken ct = default)
    {
        var result = await ApplyManyAsync(id, presetId, [targetId], ct);
        return result[0];
    }

    public async Task<IReadOnlyList<DkpTransactionDto>> ApplyManyAsync(string id, Guid presetId, IReadOnlyCollection<Guid> targetUserIds, CancellationToken ct = default)
    {
        var actor = await Actor(id, ct);
        Officer(actor);
        if (targetUserIds is null || targetUserIds.Count == 0)
            throw new ArgumentException("At least one player must be selected.", nameof(targetUserIds));

        var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
        if (!preset.IsActive)
            throw new InvalidOperationException("Preset is inactive.");

        var targetIds = targetUserIds.Distinct().ToArray();
        var targets = new List<User>(targetIds.Length);
        foreach (var targetId in targetIds)
        {
            var target = await users.FindByIdAsync(targetId, ct) ?? throw new KeyNotFoundException("One of the selected players does not exist.");
            if (target.IsBlocked)
                throw new InvalidOperationException("A selected player is blocked.");
            targets.Add(target);
        }

        return await ledger.WithUserLocksAsync(targetIds.OrderBy(idValue => idValue).ToArray(), async () =>
        {
            foreach (var target in targets)
            {
                if (await presets.GetUsageCountAsync(preset.Id, target.Id, ct) >= preset.MaxApplicationsPerUser)
                    throw new InvalidOperationException($"Preset lifetime limit reached for {target.DiscordName}.");
            }

            var now = time.GetUtcNow().UtcDateTime;
            var events = new List<DkpEvent>(targets.Count);
            var applications = new List<DkpAwardPresetApplication>(targets.Count);
            var result = new List<DkpTransactionDto>(targets.Count);

            foreach (var target in targets)
            {
                var eventId = Guid.NewGuid();
                var payload = JsonSerializer.Serialize(new
                {
                    targetUserId = target.Id,
                    amount = preset.Amount,
                    reason = preset.Reason,
                    actorUserId = actor.Id,
                    presetId = preset.Id
                });
                var transactionEvent = new DkpEvent(
                    "UserBalance",
                    target.Id,
                    await ledger.GetNextSequenceAsync("UserBalance", target.Id, ct),
                    "DkpCredited",
                    target.Id,
                    actor.Id,
                    now,
                    eventId,
                    payload);
                events.Add(transactionEvent);
                // The DKP transaction is event-sourced in DkpEvents. There is no
                // corresponding legacy DkpTransaction row for this operation.
                applications.Add(new DkpAwardPresetApplication(preset.Id, target.Id, null, actor.Id, now));
                await ledger.ApplyBalanceAsync(target.Id, preset.Amount, eventId, now, ct);
                result.Add(new DkpTransactionDto(eventId, preset.Amount, preset.Reason, now, actor.DiscordName));
            }

            await ledger.AppendAsync(events, ct);
            foreach (var application in applications)
                await presets.AddApplicationAsync(application, ct);
            await ledger.SaveChangesAsync(ct);
            await presets.SaveChangesAsync(ct);
            return result;
        }, ct);
    }
}
