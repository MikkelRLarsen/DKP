using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpAwardRequests;

public sealed class BotDkpRequestCommandService(
    IUserRepository users,
    IDkpAwardRequestRepository requests,
    IPresetRepository presets,
    IEventLedgerRepository ledger,
    INotificationOutboxRepository outbox,
    ICommandUnitOfWork unitOfWork,
    TimeProvider time) : IBotDkpRequestCommands
{
    public Task<IReadOnlyList<DkpAwardRequestDto>> CreateAsync(string discordId, Guid presetId, int quantity, IReadOnlyList<string>? targetDiscordIds, string? comment, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync<IReadOnlyList<DkpAwardRequestDto>>([], async () =>
        {
            await ActiveUserAsync(discordId, ct);
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            var normalizedComment = Normalize(comment);
            var ids = new[] { discordId }.Concat(targetDiscordIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
            var targets = new List<User>(ids.Length);
            foreach (var id in ids) targets.Add(await ActiveUserAsync(id, ct));
            foreach (var target in targets)
            {
                var state = await ledger.GetStateAsync(target.Id, ct);
                if (state.PresetUsage.GetValueOrDefault(preset.Id) + quantity > preset.MaxApplicationsPerUser)
                    throw new InvalidOperationException($"{target.DiscordName} has only {Math.Max(0, preset.MaxApplicationsPerUser - state.PresetUsage.GetValueOrDefault(preset.Id))} remaining application(s).");
            }
            var now = time.GetUtcNow().UtcDateTime;
            var result = new List<DkpAwardRequestDto>(targets.Count);
            foreach (var target in targets)
            {
                var created = new DkpAwardRequest(target.Id, preset.Id, null, quantity, normalizedComment, now);
                await requests.AddAsync(created, ct);
                await outbox.AddAsync(RequestNotificationFactory.Create(created, target.DiscordName, preset.Name, preset.Amount, "DKP", now), ct);
                result.Add(new DkpAwardRequestDto(created.Id, target.Id, target.DiscordName, null, preset.Id, null, preset.Name, preset.Amount, quantity, preset.Reason, normalizedComment, DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, now, null, null, null, []));
            }
            return result;
        }, ct);

    public Task<bool> CancelAsync(string discordId, Guid requestId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var user = await ActiveUserAsync(discordId, ct);
            var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
            if (request.UserId != user.Id) throw new UnauthorizedAccessException("You can only cancel your own request.");
            var now = time.GetUtcNow().UtcDateTime;
            request.Cancel(now);
            await outbox.RequestDeletionAsync(request.Id, now, ct);
            return true;
        }, ct);

    private async Task<User> ActiveUserAsync(string discordId, CancellationToken ct)
    {
        var user = string.IsNullOrWhiteSpace(discordId) ? null : await users.FindByDiscordIdAsync(discordId, ct);
        return user is null || user.IsBlocked ? throw new UnauthorizedAccessException("An active member is required.") : user;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 500) throw new ArgumentException("Comment must be at most 500 characters.");
        return normalized;
    }
}
