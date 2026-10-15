using System.Text.Json;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Bulletins;

public sealed class BotBulletinCommandService(
    IUserRepository users,
    IBulletinRepository bulletins,
    IEventLedgerRepository ledger,
    INotificationOutboxRepository outbox,
    ICommandUnitOfWork unitOfWork,
    TimeProvider time) : IBotBulletinCommands
{
    public Task<BulletinClaimDto> ClaimAsync(string discordId, Guid itemId, int quantity, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("An active member is required.");
            var user = await users.FindByDiscordIdAsync(discordId, ct);
            if (user is null || user.IsBlocked) throw new UnauthorizedAccessException("An active member is required.");
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");

            var item = await bulletins.FindItemAsync(itemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
            var now = time.GetUtcNow().UtcDateTime;
            var reserved = await bulletins.GetReservedQuantityAsync(item.Id, ct);
            if (!item.IsEffectivelyActive(now, reserved)) throw new InvalidOperationException("This bulletin item is no longer available.");
            if (item.UniquePerUser && quantity != 1) throw new InvalidOperationException("This item can only be claimed once per user.");
            if (item.UniquePerUser && await bulletins.HasActiveClaimAsync(item.Id, user.Id, ct)) throw new InvalidOperationException("You already have an active claim for this item.");
            if (quantity > item.MaxCompletions - reserved) throw new InvalidOperationException("The requested quantity exceeds the remaining charges.");

            var claim = new BulletinClaim(item.Id, user.Id, quantity, null, now);
            await bulletins.AddClaimAsync(claim, ct);
            await outbox.AddAsync(CreateClaimNotification(claim, item, user, now), ct);
            await SyncNotificationAsync(item, now, ct);
            return new BulletinClaimDto(claim.Id, item.Id, user.Id, user.DiscordName, user.Characters.FirstOrDefault(x => x.IsMain) is { } main ? $"{main.FirstName} {main.LastName}" : null, item.Title, quantity, null, DKP.Facade.Contracts.BulletinClaimStatus.Pending, now, null, null, null, []);
        }, ct);

    public async Task ApproveAsync(string discordId, Guid claimId, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteAsync([], async () =>
        {
            var officer = await users.FindByDiscordIdAsync(discordId, ct);
            if (officer is null || officer.IsBlocked || officer.Role != Domain.UserRole.Officer)
                throw new UnauthorizedAccessException("An active Officer is required.");

            var claim = await bulletins.FindClaimAsync(claimId, ct) ?? throw new KeyNotFoundException("Bulletin claim not found.");
            if (claim.Status != Domain.BulletinClaimStatus.Pending) throw new InvalidOperationException("This claim has already been processed.");
            var target = await users.FindByIdAsync(claim.UserId, ct) ?? throw new KeyNotFoundException("Claim user not found.");
            if (target.IsBlocked) throw new InvalidOperationException("The claimant is blocked.");
            var item = await bulletins.FindItemAsync(claim.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
            var now = time.GetUtcNow().UtcDateTime;
            var amount = checked(item.DkpAmount * claim.Quantity);
            var entry = await ledger.PostAsync(target.Id, officer.Id, Guid.NewGuid(), now, new DkpPosted(amount, item.Title), ct);
            claim.Approve(officer.Id, [entry.Id], null, now);
            if (await bulletins.GetReservedQuantityAsync(item.Id, ct) >= item.MaxCompletions && await bulletins.GetPendingQuantityAsync(item.Id, ct) == 0) item.DeactivateAutomatically(now);
            await outbox.RequestDeletionAsync(claim.Id, now, ct, "BulletinClaimCreated");
            await SyncNotificationAsync(item, now, ct);
            return true;
        }, ct);
    }

    public async Task RejectAsync(string discordId, Guid claimId, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteAsync([], async () =>
        {
            var officer = await users.FindByDiscordIdAsync(discordId, ct);
            if (officer is null || officer.IsBlocked || officer.Role != Domain.UserRole.Officer)
                throw new UnauthorizedAccessException("An active Officer is required.");

            var claim = await bulletins.FindClaimAsync(claimId, ct) ?? throw new KeyNotFoundException("Bulletin claim not found.");
            if (claim.Status != Domain.BulletinClaimStatus.Pending) throw new InvalidOperationException("This claim has already been processed.");
            var item = await bulletins.FindItemAsync(claim.BulletinItemId, ct) ?? throw new KeyNotFoundException("Bulletin item not found.");
            var now = time.GetUtcNow().UtcDateTime;
            claim.Reject(officer.Id, null, now);
            await outbox.RequestDeletionAsync(claim.Id, now, ct, "BulletinClaimCreated");
            await SyncNotificationAsync(item, now, ct);
            return true;
        }, ct);
    }

    private async Task SyncNotificationAsync(BulletinItem item, DateTime now, CancellationToken ct)
    {
        var reserved = await bulletins.GetReservedQuantityAsync(item.Id, ct);
        var pending = await bulletins.GetPendingQuantityAsync(item.Id, ct);
        var remaining = Math.Max(0, item.MaxCompletions - reserved);
        var delete = !item.IsActive || item.IsHidden || item.IsExpired(now) || (remaining == 0 && pending == 0);
        var payload = JsonSerializer.Serialize(new
        {
            BulletinItemId = item.Id,
            item.Title,
            item.Description,
            item.DkpAmount,
            item.DeadlineUtc,
            RemainingQuantity = remaining,
            PendingQuantity = pending,
            item.MaxCompletions,
            UpdatedAtUtc = now
        });
        await outbox.SyncBulletinAsync(item.Id, payload, delete, now, ct);
    }

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
}
