using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.Shop;

internal static class ShopNotificationFactory
{
    public static DiscordNotificationOutbox CreateRefund(User target, string officerName, string itemName, int quantity, int amount, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new RefundNotificationPayload(
            target.DiscordName, itemName, quantity, amount, officerName, now));
        return new DiscordNotificationOutbox("ShopPurchaseRefunded", payload, now, recipientDiscordUserId: target.DiscordId);
    }

    private sealed record RefundNotificationPayload(string UserName, string ItemName, int Quantity, int Amount, string OfficerName, DateTime RefundedAtUtc);
}
