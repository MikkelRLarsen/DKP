using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpAwardRequests;

public sealed class DkpAwardRequestCommandService(CommandContext context, IDkpAwardRequestRepository requests, IPresetRepository presets, IAchievementRepository achievements, IEventLedgerRepository ledger, INotificationOutboxRepository outbox, TimeProvider time) : IDkpAwardRequestCommands
{
    public Task<DkpAwardRequestDto> CreateAsync(CreateDkpAwardRequestRequest input, CancellationToken ct = default) => context.ExecuteAsync([], false, async actor =>
    {
        if ((input.PresetId is null) == (input.AchievementId is null)) throw new ArgumentException("Select exactly one DKP source.");
        if (input.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (input.AchievementId is not null && input.Quantity != 1) throw new ArgumentException("Achievement requests have quantity 1.");
        var comment = Normalize(input.Comment, "Comment");
        if (input.AchievementId is not null && await requests.HasPendingAsync(actor.Id, null, input.AchievementId, ct)) throw new InvalidOperationException("You already have a pending request for this achievement.");
        if (input.PresetId is Guid presetId)
        {
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            var state = await ledger.GetStateAsync(actor.Id, ct);
            if (state.PresetUsage.GetValueOrDefault(preset.Id) + input.Quantity > preset.MaxApplicationsPerUser) throw new InvalidOperationException("The requested quantity exceeds your remaining lifetime limit.");
            var request = new DkpAwardRequest(actor.Id, preset.Id, null, input.Quantity, comment, time.GetUtcNow().UtcDateTime);
            await requests.AddAsync(request, ct);
            await outbox.AddAsync(RequestNotificationFactory.Create(request, actor.DiscordName, preset.Name, preset.Amount, "DKP", request.CreatedAtUtc), ct);
            return ToDto(request, actor.DiscordName, null, preset, null);
        }
        var achievement = await achievements.FindAsync(input.AchievementId!.Value, ct) ?? throw new KeyNotFoundException("Achievement not found.");
        if (!achievement.IsActive) throw new InvalidOperationException("Achievement is inactive.");
        var achievementRequest = new DkpAwardRequest(actor.Id, null, achievement.Id, 1, comment, time.GetUtcNow().UtcDateTime);
        await requests.AddAsync(achievementRequest, ct);
        await outbox.AddAsync(RequestNotificationFactory.Create(achievementRequest, actor.DiscordName, achievement.Name, achievement.DkpAmount, "Achievement", achievementRequest.CreatedAtUtc), ct);
        return ToDto(achievementRequest, actor.DiscordName, achievement, null, null);
    }, ct);

    public Task CancelAsync(Guid requestId, CancellationToken ct = default) => context.ExecuteAsync([], false, async actor =>
    {
        var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
        if (request.UserId != actor.Id) throw new UnauthorizedAccessException("You can only cancel your own request.");
        var now = time.GetUtcNow().UtcDateTime;
        request.Cancel(now); await outbox.RequestDeletionAsync(request.Id, now, ct); return true;
    }, ct);

    public Task ApproveAsync(Guid requestId, ReviewDkpAwardRequestRequest input, CancellationToken ct = default) => ReviewAsync(requestId, input.Comment, true, ct);
    public Task RejectAsync(Guid requestId, ReviewDkpAwardRequestRequest input, CancellationToken ct = default) => ReviewAsync(requestId, input.Comment, false, ct);

    private Task<bool> ReviewAsync(Guid requestId, string? rawComment, bool approve, CancellationToken ct) => context.ExecuteAsync([requestId], true, async officer =>
    {
        var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
        var target = await context.TargetAsync(request.UserId, ct);
        var reviewComment = Normalize(rawComment, "Review comment");
        var now = time.GetUtcNow().UtcDateTime;
        var sourceName = "Unknown source";
        var sourceAmount = 0;
        var sourceKind = "DKP";
        if (request.PresetId is Guid presetForNotification)
        {
            var preset = await presets.FindAsync(presetForNotification, ct) ?? throw new KeyNotFoundException("Preset not found.");
            sourceName = preset.Name;
            sourceAmount = preset.Amount;
        }
        else if (request.AchievementId is Guid achievementForNotification)
        {
            var achievement = await achievements.FindAsync(achievementForNotification, ct) ?? throw new KeyNotFoundException("Achievement not found.");
            sourceName = achievement.Name;
            sourceAmount = achievement.DkpAmount;
            sourceKind = "Achievement";
        }
        else throw new InvalidOperationException("The request has no valid source.");

        if (!approve)
        {
            request.Reject(officer.Id, reviewComment, now);
            await outbox.AddAsync(RequestNotificationFactory.CreateReview(request, target.DiscordId, target.DiscordName, sourceName, sourceAmount, sourceKind, false, reviewComment, officer.DiscordName, now), ct);
            await outbox.RequestDeletionAsync(request.Id, now, ct);
            return true;
        }
        var operationId = Guid.NewGuid(); var eventIds = new List<Guid>(request.Quantity);
        if (request.PresetId is Guid presetId)
        {
            var preset = await presets.FindAsync(presetId, ct) ?? throw new KeyNotFoundException("Preset not found.");
            if (!preset.IsActive) throw new InvalidOperationException("Preset is inactive.");
            var state = await ledger.GetStateAsync(target.Id, ct);
            if (state.PresetUsage.GetValueOrDefault(preset.Id) + request.Quantity > preset.MaxApplicationsPerUser) throw new InvalidOperationException($"{target.DiscordName}: requested quantity exceeds the preset lifetime limit.");
            for (var i = 0; i < request.Quantity; i++) eventIds.Add((await ledger.PostAsync(target.Id, officer.Id, operationId, now, new DkpPosted(preset.Amount, preset.Reason, preset.Id), ct)).Id);
        }
        else if (request.AchievementId is Guid achievementId)
        {
            var achievement = await achievements.FindAsync(achievementId, ct) ?? throw new KeyNotFoundException("Achievement not found.");
            if (!achievement.IsActive) throw new InvalidOperationException("Achievement is inactive.");
            if (await achievements.HasActiveAsync(target.Id, achievementId, ct)) throw new InvalidOperationException($"{target.DiscordName} already has this achievement.");
            var entry = await ledger.PostAsync(target.Id, officer.Id, operationId, now, new DkpPosted(achievement.DkpAmount, achievement.Name, null, achievement.Id), ct);
            eventIds.Add(entry.Id); await achievements.AddUserAchievementAsync(new UserAchievement(target.Id, achievement.Id, officer.Id, entry.Id, now), ct);
        }
        else throw new InvalidOperationException("The request has no valid source.");
        request.Approve(officer.Id, eventIds, reviewComment, now);
        await outbox.AddAsync(RequestNotificationFactory.CreateReview(request, target.DiscordId, target.DiscordName, sourceName, sourceAmount, sourceKind, true, reviewComment, officer.DiscordName, now), ct);
        await outbox.RequestDeletionAsync(request.Id, now, ct);
        return true;
    }, ct);

    private static string? Normalize(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); if (result.Length > 500) throw new ArgumentException($"{field} must be at most 500 characters."); return result; }
    private static DkpAwardRequestDto ToDto(DkpAwardRequest request, string discordName, AchievementDefinition? achievement, DkpAwardPreset? preset, string? reviewer) => new(request.Id, request.UserId, discordName, null, request.PresetId, request.AchievementId, achievement?.Name ?? preset!.Name, achievement?.DkpAmount ?? preset!.Amount, request.Quantity, achievement?.Description ?? preset!.Reason, request.Comment, ToStatus(request.Status), request.CreatedAtUtc, request.ReviewedAtUtc, reviewer, request.ReviewComment, request.DkpEventIds);
    private static DKP.Facade.Contracts.DkpAwardRequestStatus ToStatus(DKP.Domain.DkpAwardRequestStatus status) => status switch { DKP.Domain.DkpAwardRequestStatus.Pending => DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, DKP.Domain.DkpAwardRequestStatus.Approved => DKP.Facade.Contracts.DkpAwardRequestStatus.Approved, DKP.Domain.DkpAwardRequestStatus.Rejected => DKP.Facade.Contracts.DkpAwardRequestStatus.Rejected, DKP.Domain.DkpAwardRequestStatus.Cancelled => DKP.Facade.Contracts.DkpAwardRequestStatus.Cancelled, _ => throw new InvalidOperationException("Unknown request status.") };
}
