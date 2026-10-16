using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.Shop;

internal static class ShopNotificationFactory
{
    public static DiscordNotificationOutbox CreateCatalog(ShopItem item, ShopPurchaseLimit? inheritedLimit, IReadOnlyList<string> achievementRequirements, DateTime now)
    {
        return new DiscordNotificationOutbox("ShopItem", CatalogPayload(item, inheritedLimit, achievementRequirements, now), now, item.Id);
    }

    public static string CatalogPayload(ShopItem item, ShopPurchaseLimit? inheritedLimit, IReadOnlyList<string> achievementRequirements, DateTime now)
        => JsonSerializer.Serialize(new CatalogNotificationPayload(
            item.Id, item.Key, item.Name, item.Description, item.Price, inheritedLimit?.MaxPerUser ?? item.MaxPerUser,
            item.IsActive, item.IsHidden, item.RollBonusValue, inheritedLimit?.SharedKey ?? item.PurchaseLimitKey,
            inheritedLimit?.MaxActivePerUser ?? item.MaxActivePerUser, inheritedLimit?.MaxPerDay ?? item.MaxPerDay,
            inheritedLimit?.MaxPerWeek ?? item.MaxPerWeek, inheritedLimit?.MaxPerMonth ?? item.MaxPerMonth, achievementRequirements, now));

    public static DiscordNotificationOutbox CreateRefund(User target, string officerName, string itemName, int quantity, int amount, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new RefundNotificationPayload(
            target.DiscordName, itemName, quantity, amount, officerName, now));
        return new DiscordNotificationOutbox("ShopPurchaseRefunded", payload, now, recipientDiscordUserId: target.DiscordId);
    }

    private sealed record RefundNotificationPayload(string UserName, string ItemName, int Quantity, int Amount, string OfficerName, DateTime RefundedAtUtc);
    private sealed record CatalogNotificationPayload(Guid ShopItemId, string Key, string Name, string Description, int Price,
        int MaxPerUser, bool IsActive, bool IsHidden, int? RollBonusValue, string? PurchaseLimitKey,
        int? MaxActivePerUser, int? MaxPerDay, int? MaxPerWeek, int? MaxPerMonth, IReadOnlyList<string> AchievementRequirements, DateTime UpdatedAtUtc);
}
