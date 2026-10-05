using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.Presets;

public sealed class DkpPresetCommandService(CommandContext context, IPresetRepository presets, IEventLedgerRepository ledger, TimeProvider time) : IDkpPresetCommands
{
    private static void Validate(DkpAwardPresetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 128 ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 500 ||
            input.Amount <= 0 || input.MaxApplicationsPerUser <= 0)
            throw new ArgumentException("Name (max 128), reason (max 500), positive amount and lifetime limit are required.");
    }
    private static DkpAwardPresetDto Dto(DkpAwardPreset p) => new(p.Id, p.Name, p.Amount, p.Reason, p.MaxApplicationsPerUser, p.IsActive);

    public Task<DkpAwardPresetDto> CreateAsync(DkpAwardPresetInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var preset = new DkpAwardPreset(input.Name.Trim(), input.Amount, input.Reason.Trim(), input.MaxApplicationsPerUser, time.GetUtcNow().UtcDateTime);
            await presets.AddAsync(preset, ct);
            return Dto(preset);
        }, ct);

    public Task<DkpAwardPresetDto> UpdateAsync(Guid presetId, DkpAwardPresetInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            preset.Update(input.Name.Trim(), input.Amount, input.Reason.Trim(), input.MaxApplicationsPerUser, time.GetUtcNow().UtcDateTime);
            return Dto(preset);
        }, ct);

    public Task SetActiveAsync(Guid presetId, bool active, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            preset.SetActive(active, time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);

    public async Task<DkpTransactionDto> ApplyAsync(Guid targetId, Guid presetId, CancellationToken ct = default)
        => (await ApplyManyAsync(presetId, [targetId], ct))[0];

    public Task<IReadOnlyList<DkpTransactionDto>> ApplyManyAsync(Guid presetId, IReadOnlyCollection<Guid> targetUserIds, CancellationToken ct = default)
    {
        var ids = CommandContext.Targets(targetUserIds);
        return context.ExecuteAsync<IReadOnlyList<DkpTransactionDto>>(ids, true, async actor =>
        {
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            foreach (var id in ids)
            {
                var target = await context.TargetAsync(id, ct);
                if (await presets.GetUsageCountAsync(presetId, id, ct) >= preset.MaxApplicationsPerUser)
                    throw new InvalidOperationException($"{target.DiscordName}: preset lifetime limit reached.");
            }
            var operationId = Guid.NewGuid();
            var now = time.GetUtcNow().UtcDateTime;
            var result = new List<DkpTransactionDto>();
            foreach (var id in ids)
            {
                var entry = await ledger.PostAsync(id, actor.Id, operationId, now, new DkpPosted(preset.Amount, preset.Reason, preset.Id), ct);
                result.Add(new(entry.Id, preset.Amount, preset.Reason, now, actor.DiscordName));
            }
            return result;
        }, ct);
    }
}
