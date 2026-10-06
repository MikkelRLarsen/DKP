using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IShopRepository
{
    Task<ShopItem?> FindItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddItemAsync(ShopItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAchievementRequirementIdsAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task ReplaceAchievementRequirementsAsync(Guid itemId, IReadOnlyCollection<Guid> achievementIds, CancellationToken cancellationToken = default);
}
