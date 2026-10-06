using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;

internal static class ReadModels
{
    public static async Task<LedgerReplayState> StateAsync(DkpDbContext db, Guid? userId, CancellationToken ct)
    {
        var query = db.DkpEvents.AsNoTracking();
        if (userId is Guid id) query = query.Where(x => x.UserId == id);
        return LedgerReplayState.Replay(await query.ToArrayAsync(ct));
    }
    public static IQueryable<CharacterDto> Characters(DkpDbContext db, Guid id) => db.Characters.Where(x => x.UserId == id).OrderByDescending(x => x.IsMain).ThenBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).Select(x => new CharacterDto(x.Id, x.FirstName, x.LastName, x.IsMain));
    public static IQueryable<UserSummary> Users(DkpDbContext db) => db.Users.OrderBy(x => x.DiscordName).ThenBy(x => x.Id).Select(x => new UserSummary(x.Id, x.DiscordId, x.DiscordName, x.AvatarUrl, Role(x.Role), x.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(), x.IsBlocked, x.BlockedAtUtc, x.BlockReason));
    public static IQueryable<ShopItemDto> Items(DkpDbContext db, bool activeOnly = false) => db.ShopItems.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new ShopItemDto(x.Id, x.Key, x.Name, x.Description, x.Price, x.MaxPerUser, x.IsActive));
    public static async Task<DkpHistoryDto> HistoryAsync(DkpDbContext db, Guid id, CancellationToken ct)
    {
        var state = await StateAsync(db, id, ct);
        var actorNames = await db.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.DiscordName, ct);
        var entries = state.Activities.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.EventId).Select(x => new DkpTransactionDto(x.EventId, x.Amount, x.Reason, x.CreatedAtUtc, actorNames.GetValueOrDefault(x.ActorUserId, "Unknown"))).ToArray();
        return new(new(state.Balance), entries);
    }
    public static async Task<IReadOnlyList<ShopPurchaseDto>> PurchasesAsync(DkpDbContext db, Guid? userId, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).ToDictionaryAsync(x => x.Id, ct);
        var events = await db.DkpEvents.AsNoTracking().Where(x => userId == null || x.UserId == userId).ToArrayAsync(ct);
        var state = LedgerReplayState.Replay(events);
        return state.Purchases.Values.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.PurchaseId).Select(p => { var user = users[p.UserId]; var main = user.Characters.FirstOrDefault(x => x.IsMain); return new ShopPurchaseDto(p.PurchaseId, p.UserId, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", p.ShopItemId, p.ItemName, p.Quantity, p.TotalDkpCost, p.CreatedAtUtc, p.CancelledAtUtc); }).ToArray();
    }
    public static Facade.Contracts.UserRole Role(Domain.UserRole role) => role switch { Domain.UserRole.Member => Facade.Contracts.UserRole.Member, Domain.UserRole.Officer => Facade.Contracts.UserRole.Officer, _ => throw new InvalidOperationException("Unknown user role.") };
}
