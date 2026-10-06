using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpAwardRequests;

public sealed class DkpAwardRequestCommandService(CommandContext context, IDkpAwardRequestRepository requests, IPresetRepository presets, IEventLedgerRepository ledger, TimeProvider time) : IDkpAwardRequestCommands
{
    public Task<DkpAwardRequestDto> CreateAsync(CreateDkpAwardRequestRequest input, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            var preset = await presets.FindAsync(input.PresetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            if (input.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
            var comment = Normalize(input.Comment, "Comment");
            if (await requests.HasPendingAsync(actor.Id, preset.Id, ct)) throw new InvalidOperationException("You already have a pending request for this source.");
            var state = await ledger.GetStateAsync(actor.Id, ct);
            if (state.PresetUsage.GetValueOrDefault(preset.Id) + input.Quantity > preset.MaxApplicationsPerUser) throw new InvalidOperationException("The requested quantity exceeds your remaining lifetime limit.");
            var request = new DkpAwardRequest(actor.Id, preset.Id, input.Quantity, comment, time.GetUtcNow().UtcDateTime);
            await requests.AddAsync(request, ct);
            return ToDto(request, actor.DiscordName, preset, null);
        }, ct);

    public Task CancelAsync(Guid requestId, CancellationToken ct = default)
        => context.ExecuteAsync([], false, async actor =>
        {
            var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
            if (request.UserId != actor.Id) throw new UnauthorizedAccessException("You can only cancel your own request.");
            request.Cancel(time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);

    public Task ApproveAsync(Guid requestId, ReviewDkpAwardRequestRequest input, CancellationToken ct = default) => ReviewAsync(requestId, input.Comment, true, ct);
    public Task RejectAsync(Guid requestId, ReviewDkpAwardRequestRequest input, CancellationToken ct = default) => ReviewAsync(requestId, input.Comment, false, ct);

    private Task<bool> ReviewAsync(Guid requestId, string? rawComment, bool approve, CancellationToken ct)
        => context.ExecuteAsync([requestId], true, async officer =>
        {
            var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
            var target = await context.TargetAsync(request.UserId, ct);
            var reviewComment = Normalize(rawComment, "Review comment");
            if (!approve)
            {
                request.Reject(officer.Id, reviewComment, time.GetUtcNow().UtcDateTime);
                return true;
            }
            var preset = await presets.FindAsync(request.PresetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            var state = await ledger.GetStateAsync(target.Id, ct);
            if (state.PresetUsage.GetValueOrDefault(preset.Id) + request.Quantity > preset.MaxApplicationsPerUser) throw new InvalidOperationException($"{target.DiscordName}: requested quantity exceeds the preset lifetime limit.");
            var now = time.GetUtcNow().UtcDateTime;
            var operationId = Guid.NewGuid();
            var eventIds = new List<Guid>(request.Quantity);
            for (var i = 0; i < request.Quantity; i++)
            {
                var entry = await ledger.PostAsync(target.Id, officer.Id, operationId, now, new DkpPosted(preset.Amount, preset.Reason, preset.Id), ct);
                eventIds.Add(entry.Id);
            }
            request.Approve(officer.Id, eventIds, reviewComment, now);
            return true;
        }, ct);

    private static string? Normalize(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        if (result.Length > 500) throw new ArgumentException($"{field} must be at most 500 characters.");
        return result;
    }

    private static DkpAwardRequestDto ToDto(DkpAwardRequest request, string discordName, DkpAwardPreset preset, string? reviewer)
        => new(request.Id, request.UserId, discordName, null, request.PresetId, preset.Name, preset.Amount, request.Quantity, preset.Reason, request.Comment, ToStatus(request.Status), request.CreatedAtUtc, request.ReviewedAtUtc, reviewer, request.ReviewComment, request.DkpEventIds);

    private static DKP.Facade.Contracts.DkpAwardRequestStatus ToStatus(DKP.Domain.DkpAwardRequestStatus status) => status switch
    {
        DKP.Domain.DkpAwardRequestStatus.Pending => DKP.Facade.Contracts.DkpAwardRequestStatus.Pending,
        DKP.Domain.DkpAwardRequestStatus.Approved => DKP.Facade.Contracts.DkpAwardRequestStatus.Approved,
        DKP.Domain.DkpAwardRequestStatus.Rejected => DKP.Facade.Contracts.DkpAwardRequestStatus.Rejected,
        DKP.Domain.DkpAwardRequestStatus.Cancelled => DKP.Facade.Contracts.DkpAwardRequestStatus.Cancelled,
        _ => throw new InvalidOperationException("Unknown request status.")
    };
}
