using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

/// <summary>The only projection algorithm, shared by append and rebuild.</summary>
public static class LedgerProjector
{
    public static async Task ApplyAsync(DkpDbContext db, DkpEvent entry, CancellationToken ct)
    {
        var payload = LedgerEvents.Read(entry);
        if (await db.LedgerEntries.AnyAsync(x => x.EventId == entry.Id, ct))
            throw new InvalidOperationException($"Event {entry.Id} was already projected.");
        var last = await db.LedgerEntries.Where(x => x.UserId == entry.UserId).MaxAsync(x => (long?)x.Sequence, ct) ?? 0;
        if (entry.Sequence != last + 1) throw new InvalidOperationException($"Broken event sequence for {entry.UserId}.");
        var balance = await db.DkpBalanceProjections.FindAsync([entry.UserId], ct);
        if (balance == null) { balance = new(entry.UserId); db.DkpBalanceProjections.Add(balance); }
        string? itemName = null;
        int? quantity = null;
        switch (payload)
        {
            case DkpPosted { PresetId: Guid presetId }:
                db.DkpAwardPresetApplications.Add(new(presetId, entry));
                break;
            case PurchasePlaced purchase:
                if (await db.ShopPurchaseProjections.AnyAsync(x => x.PurchaseId == purchase.PurchaseId, ct))
                    throw new InvalidOperationException("Duplicate purchase identity.");
                if (balance.Balance < -purchase.Amount) throw new InvalidOperationException("Purchase exceeds available balance.");
                if (purchase.RollBonusValue != null && await db.ShopPurchaseProjections.AnyAsync(x => x.UserId == entry.UserId && x.CancelledAtUtc == null && x.RollBonusValue != null, ct))
                    throw new InvalidOperationException("Multiple active RollBonus purchases.");
                db.ShopPurchaseProjections.Add(new(entry, purchase));
                itemName = purchase.ItemName; quantity = purchase.Quantity;
                break;
            case PurchaseCancelled cancellation:
                var existing = await db.ShopPurchaseProjections.FindAsync([cancellation.PurchaseId], ct)
                    ?? throw new InvalidOperationException("Cancellation without purchase.");
                if (existing.UserId != entry.UserId || existing.TotalDkpCost != cancellation.Amount)
                    throw new InvalidOperationException("Cancellation does not match original purchase.");
                existing.Cancel(entry);
                itemName = existing.ItemName; quantity = existing.Quantity;
                break;
        }
        balance.Apply(payload.Amount, entry.Id, entry.OccurredAtUtc);
        db.LedgerEntries.Add(new(entry, payload, itemName, quantity));
        // Persist inside the surrounding transaction so the next sequence sees this event.
        await db.SaveChangesAsync(ct);
    }
}
