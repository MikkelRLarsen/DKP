using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;

public sealed class EventLedgerRepository(CommandUnitOfWork session) : IEventLedgerRepository
{
    private DkpDbContext Db => session.Db;
    public async Task<int> GetBalanceAsync(Guid userId, CancellationToken ct = default)
        => await Db.DkpBalanceProjections.Where(x => x.UserId == userId).Select(x => (int?)x.Balance).SingleOrDefaultAsync(ct) ?? 0;
    public Task<int> GetActiveQuantityAsync(Guid userId, Guid itemId, CancellationToken ct = default)
        => Db.ShopPurchaseProjections.Where(x => x.UserId == userId && x.ShopItemId == itemId && x.CancelledAtUtc == null).SumAsync(x => x.Quantity, ct);
    public Task<bool> HasActiveRollBonusAsync(Guid userId, CancellationToken ct = default)
        => Db.ShopPurchaseProjections.AnyAsync(x => x.UserId == userId && x.CancelledAtUtc == null && x.RollBonusValue != null, ct);
    public Task<ShopPurchaseProjection?> GetPurchaseAsync(Guid purchaseId, CancellationToken ct = default)
        => Db.ShopPurchaseProjections.SingleOrDefaultAsync(x => x.PurchaseId == purchaseId, ct);
    public async Task<DkpEvent> PostAsync(Guid userId, Guid actorId, Guid operationId, DateTime now, ILedgerPayload payload, CancellationToken ct = default)
    {
        var sequence = (await Db.DkpEvents.Where(x => x.UserId == userId).MaxAsync(x => (long?)x.Sequence, ct) ?? 0) + 1;
        var entry = LedgerEvents.Create(userId, actorId, sequence, operationId, now, payload);
        Db.DkpEvents.Add(entry);
        await LedgerProjector.ApplyAsync(Db, entry, ct);
        return entry;
    }
}
