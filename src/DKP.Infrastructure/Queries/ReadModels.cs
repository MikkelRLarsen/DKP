using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;

internal static class ReadModels
{
    public static Task<int> BalanceAsync(DkpDbContext db, Guid id, CancellationToken ct)
        => db.DkpBalanceProjections.Where(x => x.UserId == id).Select(x => x.Balance).SingleOrDefaultAsync(ct);
    public static IQueryable<CharacterDto> Characters(DkpDbContext db, Guid id)
        => db.Characters.Where(x => x.UserId == id).OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Select(x => new CharacterDto(x.Id, x.FirstName, x.LastName, x.IsMain));
    public static IQueryable<UserSummary> Users(DkpDbContext db)
        => db.Users.OrderBy(x => x.DiscordName).ThenBy(x => x.Id).Select(x => new UserSummary(
            x.Id, x.DiscordId, x.DiscordName, x.AvatarUrl, Role(x.Role),
            x.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(),
            x.IsBlocked, x.BlockedAtUtc, x.BlockReason));
    public static IQueryable<ShopItemDto> Items(DkpDbContext db, bool activeOnly = false)
        => db.ShopItems.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new ShopItemDto(x.Id, x.Key, x.Name, x.Description, x.Price, x.MaxPerUser, x.IsActive));
    public static IQueryable<ShopPurchaseDto> Purchases(DkpDbContext db, Guid? userId = null)
        => from p in db.ShopPurchaseProjections.Where(x => userId == null || x.UserId == userId)
           join u in db.Users on p.UserId equals u.Id
           orderby p.CreatedAtUtc descending, p.PurchaseId descending
           select new ShopPurchaseDto(p.PurchaseId, p.UserId, u.DiscordName,
               u.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(),
               p.ShopItemId, p.ItemName, p.Quantity, p.TotalDkpCost, p.CreatedAtUtc, p.CancelledAtUtc);
    public static async Task<DkpHistoryDto> HistoryAsync(DkpDbContext db, Guid id, CancellationToken ct)
    {
        var entries = await (from e in db.LedgerEntries
            join actor in db.Users on e.ActorUserId equals actor.Id
            where e.UserId == id
            orderby e.CreatedAtUtc descending, e.Sequence descending, e.EventId descending
            select new DkpTransactionDto(e.EventId, e.Amount, e.Reason, e.CreatedAtUtc, actor.DiscordName)).ToArrayAsync(ct);
        return new(new(await BalanceAsync(db, id, ct)), entries);
    }
    public static Facade.Contracts.UserRole Role(Domain.UserRole role) => role switch
    {
        Domain.UserRole.Member => Facade.Contracts.UserRole.Member,
        Domain.UserRole.Officer => Facade.Contracts.UserRole.Officer,
        _ => throw new InvalidOperationException("Unknown user role.")
    };
}
