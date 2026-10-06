namespace DKP.Domain;

public sealed class ShopItemAchievementRequirement
{
    private ShopItemAchievementRequirement() { }
    public ShopItemAchievementRequirement(Guid shopItemId, Guid achievementId) { ShopItemId = shopItemId; AchievementId = achievementId; }
    public Guid ShopItemId { get; private set; }
    public Guid AchievementId { get; private set; }
}
