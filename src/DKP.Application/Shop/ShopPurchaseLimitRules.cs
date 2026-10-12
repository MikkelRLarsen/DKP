using DKP.Domain;

namespace DKP.Application.Shop;

public static class ShopPurchaseLimitRules
{
    public static bool UsesActiveLimit(ShopItem item)
        => item.Key == "soft-reserve" || item.RollBonusValue is not null;

    public static int OwnedQuantity(LedgerReplayState state, ShopItem item)
    {
        var purchases = state.Purchases.Values.Where(p => p.ShopItemId == item.Id && p.CancelledAtUtc is null);
        return UsesActiveLimit(item) ? purchases.Sum(p => p.ActiveQuantity) : purchases.Sum(p => p.Quantity);
    }
}
