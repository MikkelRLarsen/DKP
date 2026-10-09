using System.Text;
using Discord.Interactions;

namespace DKP.DiscordBot;

[Group("shop", "View and purchase available DKP shop items")]
public sealed class ShopModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "List the active DKP shop items", runMode: RunMode.Async)]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);
        var items = await api.GetShopItemsAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (items is null) { await ModifyOriginalResponseAsync(p => p.Content = "The DKP shop could not be loaded."); return; }
        if (items.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "There are currently no shop items available."); return; }
        var output = new StringBuilder("**Available DKP shop items**\n");
        foreach (var item in items)
        {
            var requirements = item.AchievementRequirements is { Count: > 0 } ? $" | Requires: {string.Join(", ", item.AchievementRequirements.Select(x => x.AchievementName))}" : string.Empty;
            output.AppendLine($"`{item.Key}` — **{item.Name}**: {item.Price} DKP each, maximum {item.MaxPerUser}{requirements}");
            if (!string.IsNullOrWhiteSpace(item.Description)) output.AppendLine($"  {item.Description}");
        }
        output.AppendLine("\nUse `/shop buy` with the item key and quantity to purchase.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("buy", "Purchase an item from the DKP shop", runMode: RunMode.Async)]
    public async Task BuyAsync([Summary("item", "The item key shown by /shop list")] string itemKey, [Summary("quantity", "How many units to purchase")] int quantity)
    {
        await DeferAsync(ephemeral: true);
        if (quantity <= 0) { await ModifyOriginalResponseAsync(p => p.Content = "Quantity must be greater than zero."); return; }
        var items = await api.GetShopItemsAsync(Context.User.Id.ToString(), CancellationToken.None);
        var item = items?.SingleOrDefault(x => string.Equals(x.Key, itemKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (item is null) { await ModifyOriginalResponseAsync(p => p.Content = "That shop item was not found. Use `/shop list` to see valid item keys."); return; }
        var purchase = await api.PurchaseAsync(Context.User.Id.ToString(), new ShopPurchaseInput(item.Id, quantity), CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = purchase is null ? "The purchase could not be completed. Check your DKP, item limit and achievement requirements." : $"Purchase completed: **{purchase.Quantity} x {purchase.ItemName}** for **{purchase.TotalDkpCost} DKP**.");
    }
}

public sealed class PurchasesModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("purchases", "Show your DKP shop purchases", runMode: RunMode.Async)]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);
        var purchases = await api.GetPurchasesAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (purchases is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your purchases could not be loaded."); return; }
        if (purchases.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "You have no shop purchases."); return; }
        var output = new StringBuilder("**Your shop purchases**\n");
        foreach (var purchase in purchases)
        {
            var status = purchase.IsCancelled ? "cancelled" : "active";
            output.AppendLine($"`{purchase.Id}` — **{purchase.Quantity} x {purchase.ItemName}**, {purchase.TotalDkpCost} DKP, {status}");
        }
        output.AppendLine("\nUse `/purchases-cancel` with a purchase ID to cancel an active purchase and receive a refund.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("purchases-cancel", "Cancel one of your active shop purchases", runMode: RunMode.Async)]
    public async Task CancelAsync([Summary("purchase_id", "The purchase ID shown by /purchases")] string purchaseId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(purchaseId, out var id)) { await ModifyOriginalResponseAsync(p => p.Content = "The purchase ID is not valid."); return; }
        var cancelled = await api.CancelPurchaseAsync(Context.User.Id.ToString(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = cancelled ? "Purchase cancelled and DKP refunded." : "The purchase could not be cancelled. It may already be cancelled or unavailable.");
    }
}
