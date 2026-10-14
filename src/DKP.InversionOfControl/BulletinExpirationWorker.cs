using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DKP.InversionOfControl;

internal sealed class BulletinExpirationWorker(IDbContextFactory<DkpDbContext> factory, TimeProvider time, ILogger<BulletinExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogWarning("Bulletin expiration worker started. The first expiration check will run now.");
        await DeactivateExpiredItemsAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogWarning("Bulletin expiration worker tick at {UtcNow}.", time.GetUtcNow().UtcDateTime);
            await DeactivateExpiredItemsAsync(stoppingToken);
        }
    }

    private async Task DeactivateExpiredItemsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(stoppingToken);
            var now = time.GetUtcNow().UtcDateTime;
            var expiredItemIds = await db.BulletinItems
                .Where(x => x.IsActive && x.DeadlineUtc.HasValue && x.DeadlineUtc.Value <= now)
                .Select(x => x.Id)
                .ToArrayAsync(stoppingToken);
            var expiredCount = await db.BulletinItems
                .Where(x => x.IsActive && x.DeadlineUtc.HasValue && x.DeadlineUtc.Value <= now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.IsAutoDeactivated, true)
                    .SetProperty(x => x.UpdatedAtUtc, now), stoppingToken);
            if (expiredCount > 0) logger.LogWarning("Deactivated {Count} expired bulletin item(s) at {UtcNow}.", expiredCount, now);
            var items = await db.BulletinItems.Where(x => x.IsActive).ToArrayAsync(stoppingToken);
            var itemIds = items.Select(x => x.Id).ToArray();
            var claims = await db.BulletinClaims
                .Where(x => itemIds.Contains(x.BulletinItemId) && (x.Status == DKP.Domain.BulletinClaimStatus.Pending || x.Status == DKP.Domain.BulletinClaimStatus.Approved))
                .ToArrayAsync(stoppingToken);
            foreach (var item in items)
            {
                var itemClaims = claims.Where(x => x.BulletinItemId == item.Id).ToArray();
                var pending = itemClaims.Where(x => x.Status == DKP.Domain.BulletinClaimStatus.Pending).Sum(x => x.Quantity);
                var approved = itemClaims.Where(x => x.Status == DKP.Domain.BulletinClaimStatus.Approved).Sum(x => x.Quantity);
                if ((item.DeadlineUtc is DateTime deadline && deadline <= now) || (pending == 0 && approved >= item.MaxCompletions)) item.DeactivateAutomatically(now);
            }

            var deactivatedItemIds = expiredItemIds
                .Concat(items.Where(x => !x.IsActive).Select(x => x.Id))
                .Distinct()
                .ToArray();
            if (deactivatedItemIds.Length > 0)
            {
                var notifications = await db.DiscordNotificationOutbox
                    .Where(x => x.NotificationType == "BulletinItem" && x.RequestId.HasValue && deactivatedItemIds.Contains(x.RequestId.Value))
                    .ToArrayAsync(stoppingToken);
                foreach (var notification in notifications) notification.RequestDeletion(now);
            }
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogWarning(exception, "Unable to deactivate expired bulletin items."); }
    }
}
