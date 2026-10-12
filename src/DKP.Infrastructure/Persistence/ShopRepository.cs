using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class ShopRepository(CommandUnitOfWork session) : IShopRepository
{
    public Task<ShopItem?> FindItemAsync(Guid id, CancellationToken ct = default) => session.Db.ShopItems.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddItemAsync(ShopItem item, CancellationToken ct = default) { session.Db.ShopItems.Add(item); return Task.CompletedTask; }
    public async Task<IReadOnlyList<Guid>> GetAchievementRequirementIdsAsync(Guid itemId, CancellationToken ct = default) => await session.Db.ShopItemAchievementRequirements.Where(x => x.ShopItemId == itemId).Select(x => x.AchievementId).ToArrayAsync(ct);
    public async Task ReplaceAchievementRequirementsAsync(Guid itemId, IReadOnlyCollection<Guid> achievementIds, CancellationToken ct = default)
    {
        var existing = await session.Db.ShopItemAchievementRequirements.Where(x => x.ShopItemId == itemId).ToArrayAsync(ct);
        session.Db.ShopItemAchievementRequirements.RemoveRange(existing);
        session.Db.ShopItemAchievementRequirements.AddRange(achievementIds.Select(x => new ShopItemAchievementRequirement(itemId, x)));
    }
}
