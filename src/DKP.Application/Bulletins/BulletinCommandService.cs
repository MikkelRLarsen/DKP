using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using System.Text.Json;

namespace DKP.Application.Bulletins;

public sealed class BulletinCommandService(CommandContext context, IBulletinRepository repository, IBulletinTemplateRepository templates, IEventLedgerRepository ledger, INotificationOutboxRepository outbox, TimeProvider time) : IBulletinCommands
{
    public Task<BulletinItemDto> CreateItemAsync(CreateBulletinItemRequest input, CancellationToken ct = default) => context.ExecuteAsync([], true, async actor =>
    {
        if (input.TemplateId is Guid templateId)
        {
            var template = await templates.FindAsync(templateId, ct) ?? throw new KeyNotFoundException("Bulletin template not found.");
            if (!template.IsActive) throw new InvalidOperationException("Inactive bulletin templates cannot be used.");
        }
        var now = time.GetUtcNow().UtcDateTime;
        var item = new BulletinItem(input.Title, input.Description, input.DkpAmount, input.MaxCompletions, input.UniquePerUser, input.DeadlineUtc, now);
        await repository.AddItemAsync(item, ct);
        await SyncNotificationAsync(item, now, ct);
        return ToItemDto(item, 0, 0, now);
    }, ct);

    public Task<BulletinItemDto> UpdateItemAsync(Guid itemId, UpdateBulletinItemRequest input, CancellationToken ct = default) => context.ExecuteAsync([], true, async actor =>
    {
        var item = await repository.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var reserved = await repository.GetReservedQuantityAsync(itemId, ct);
        item.Update(input.Title, input.Description, input.DkpAmount, input.MaxCompletions, input.UniquePerUser, input.DeadlineUtc, time.GetUtcNow().UtcDateTime, reserved);
        var now = time.GetUtcNow().UtcDateTime;
        item.RestoreAfterDeadlineExtension(now, reserved);
        await SyncNotificationAsync(item, now, ct);
        return ToItemDto(item, reserved, 0, now);
    }, ct);

    public Task SetItemActiveAsync(Guid itemId, bool isActive, CancellationToken ct = default) => context.ExecuteAsync([], true, async actor =>
    {
        var item = await repository.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var now = time.GetUtcNow().UtcDateTime;
        item.SetActive(isActive, now);
        await SyncNotificationAsync(item, now, ct);
        return true;
    }, ct);

    public Task SetItemHiddenAsync(Guid itemId, bool isHidden, CancellationToken ct = default) => context.ExecuteAsync([], true, async actor =>
    {
        var item = await repository.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var now = time.GetUtcNow().UtcDateTime;
        item.SetHidden(isHidden, now);
        await SyncNotificationAsync(item, now, ct);
        return true;
    }, ct);

    public Task<BulletinClaimDto> ClaimAsync(CreateBulletinClaimRequest input, CancellationToken ct = default) => context.ExecuteAsync([], false, async actor =>
    {
        if (input.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        var item = await repository.FindItemAsync(input.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var now = time.GetUtcNow().UtcDateTime;
        var reserved = await repository.GetReservedQuantityAsync(item.Id, ct);
        if (!item.IsEffectivelyActive(now, reserved)) throw new InvalidOperationException("This bulletin item is no longer available.");
        if (item.UniquePerUser && await repository.HasActiveClaimAsync(item.Id, actor.Id, ct)) throw new InvalidOperationException("You already have an active claim for this item.");
        if (input.Quantity > item.MaxCompletions - reserved) throw new InvalidOperationException("The requested quantity exceeds the remaining charges.");
        var claim = new BulletinClaim(item.Id, actor.Id, input.Quantity, input.Comment, now);
        await repository.AddClaimAsync(claim, ct);
        await outbox.AddAsync(CreateClaimNotification(claim, item, actor, now), ct);
        await SyncNotificationAsync(item, now, ct);
        return ToClaimDto(claim, actor, item, null);
    }, ct);

    public Task CancelClaimAsync(Guid claimId, CancellationToken ct = default) => context.ExecuteAsync([], false, async actor =>
    {
        var claim = await repository.FindClaimAsync(claimId, ct) ?? throw new KeyNotFoundException("Bulletin claim not found.");
        if (claim.UserId != actor.Id) throw new UnauthorizedAccessException("You can only cancel your own claim.");
        var now = time.GetUtcNow().UtcDateTime;
        claim.Cancel(now);
        var item = await repository.FindItemAsync(claim.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        await outbox.RequestDeletionAsync(claim.Id, now, ct, "BulletinClaimCreated");
        await SyncNotificationAsync(item, now, ct);
        return true;
    }, ct);

    public Task ApproveClaimAsync(Guid claimId, ReviewBulletinClaimRequest input, CancellationToken ct = default) => ReviewAsync(claimId, input.Comment, true, ct);
    public Task RejectClaimAsync(Guid claimId, ReviewBulletinClaimRequest input, CancellationToken ct = default) => ReviewAsync(claimId, input.Comment, false, ct);
    public Task RevertApprovedClaimAsync(Guid claimId, ReviewBulletinClaimRequest input, CancellationToken ct = default) => context.ExecuteAsync([], true, async officer =>
    {
        var claim = await repository.FindClaimAsync(claimId, ct) ?? throw new KeyNotFoundException("Bulletin claim not found.");
        var target = await context.TargetAsync(claim.UserId, ct);
        var item = await repository.FindItemAsync(claim.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var now = time.GetUtcNow().UtcDateTime;
        var amount = checked(item.DkpAmount * claim.Quantity);
        var entry = await ledger.PostAsync(target.Id, officer.Id, Guid.NewGuid(), now, new DkpPosted(-amount, $"Reverted bulletin claim: {item.Title}"), ct);
        claim.Revert(officer.Id, entry.Id, input.Comment, now);
        await SyncNotificationAsync(item, now, ct);
        return true;
    }, ct);

    private Task<bool> ReviewAsync(Guid claimId, string? comment, bool approve, CancellationToken ct) => context.ExecuteAsync([], true, async officer =>
    {
        var claim = await repository.FindClaimAsync(claimId, ct) ?? throw new KeyNotFoundException("Bulletin claim not found.");
        var target = await context.TargetAsync(claim.UserId, ct);
        var item = await repository.FindItemAsync(claim.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
        var now = time.GetUtcNow().UtcDateTime;
        if (!approve)
        {
            claim.Reject(officer.Id, comment, now);
            await outbox.RequestDeletionAsync(claim.Id, now, ct, "BulletinClaimCreated");
            await SyncNotificationAsync(item, now, ct);
            return true;
        }
        var operationId = Guid.NewGuid();
        var amount = checked(item.DkpAmount * claim.Quantity);
        var entry = await ledger.PostAsync(target.Id, officer.Id, operationId, now, new DkpPosted(amount, item.Title), ct);
        claim.Approve(officer.Id, [entry.Id], comment, now);
        await outbox.RequestDeletionAsync(claim.Id, now, ct, "BulletinClaimCreated");
        if (await repository.GetReservedQuantityAsync(item.Id, ct) >= item.MaxCompletions && await repository.GetPendingQuantityAsync(item.Id, ct) == 0) item.DeactivateAutomatically(now);
        await SyncNotificationAsync(item, now, ct);
        return true;
    }, ct);

    private async Task SyncNotificationAsync(BulletinItem item, DateTime now, CancellationToken ct)
    {
        var reserved = await repository.GetReservedQuantityAsync(item.Id, ct);
        var pending = await repository.GetPendingQuantityAsync(item.Id, ct);
        var remaining = Math.Max(0, item.MaxCompletions - reserved);
        var delete = !item.IsActive || item.IsHidden || item.IsExpired(now) || (remaining == 0 && pending == 0);
        var payload = JsonSerializer.Serialize(new BulletinNotificationPayload(
            item.Id, item.Title, item.Description, item.DkpAmount, item.DeadlineUtc, remaining, pending, item.MaxCompletions, now));
        await outbox.SyncBulletinAsync(item.Id, payload, delete, now, ct);
    }

    private sealed record BulletinNotificationPayload(Guid BulletinItemId, string Title, string Description, int DkpAmount, DateTime? DeadlineUtc, int RemainingQuantity, int PendingQuantity, int MaxCompletions, DateTime UpdatedAtUtc);

    private static DiscordNotificationOutbox CreateClaimNotification(BulletinClaim claim, BulletinItem item, User user, DateTime now)
        => new("BulletinClaimCreated", JsonSerializer.Serialize(new
        {
            ClaimId = claim.Id,
            BulletinItemId = item.Id,
            item.Title,
            item.DkpAmount,
            UserName = user.DiscordName,
            Quantity = claim.Quantity,
            claim.CreatedAtUtc
        }), now, claim.Id);

    private static BulletinItemDto ToItemDto(BulletinItem item, int reserved, int approved, DateTime now) => new(item.Id, item.Title, item.Description, item.DkpAmount, item.MaxCompletions, reserved - approved, approved, Math.Max(0, item.MaxCompletions - reserved), item.UniquePerUser, item.DeadlineUtc, item.IsActive, item.IsEffectivelyActive(now, reserved), item.CreatedAtUtc, item.UpdatedAtUtc, item.IsHidden, false);
    private static BulletinClaimDto ToClaimDto(BulletinClaim claim, User user, BulletinItem item, string? reviewer) => new(claim.Id, item.Id, user.Id, user.DiscordName, user.Characters.FirstOrDefault(x => x.IsMain) is { } main ? $"{main.FirstName} {main.LastName}" : null, item.Title, claim.Quantity, claim.Comment, (DKP.Facade.Contracts.BulletinClaimStatus)claim.Status, claim.CreatedAtUtc, claim.ReviewedAtUtc, reviewer, claim.ReviewComment, claim.DkpEventIds);
}
