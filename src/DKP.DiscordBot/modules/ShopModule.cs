using System.Text;
using Discord.Interactions;

namespace DKP.DiscordBot.modules;

[Group("shop", "View and purchase available DKP shop items")]
public sealed class ShopModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "List the active DKP shop items", runMode: RunMode.Async)]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);
        var items = await api.GetShopAvailabilityAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (items is null) { await ModifyOriginalResponseAsync(p => p.Content = "The DKP shop could not be loaded."); return; }
        if (items.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "There are currently no shop items available."); return; }
        var output = new StringBuilder("**Available DKP shop items**\n");
        foreach (var item in items)
        {
            var requirements = item.AchievementRequirements is { Count: > 0 } ? $" | Requires: {string.Join(", ", item.AchievementRequirements.Select(x => x.AchievementName))}" : string.Empty;
            output.AppendLine($"**{item.Name}**: {item.Price} DKP each, {item.RemainingQuantity} available of {item.MaxPerUser}{(item.IsAvailable ? string.Empty : " (unavailable)")}{requirements}");
            if (!string.IsNullOrWhiteSpace(item.Description)) output.AppendLine($"  {item.Description}");
        }
        output.AppendLine("\nUse `/shop buy` and select an item from the dropdown, then enter the quantity.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("buy", "Purchase an item from the DKP shop", runMode: RunMode.Async)]
    public async Task BuyAsync([Summary("item", "Select a shop item")] [Autocomplete<ShopItemAutocompleteHandler>] string itemKey, [Summary("quantity", "How many units to purchase")] int quantity)
    {
        await DeferAsync(ephemeral: true);
        if (quantity <= 0) { await ModifyOriginalResponseAsync(p => p.Content = "Quantity must be greater than zero."); return; }
        var items = await api.GetShopAvailabilityAsync(Context.User.Id.ToString(), CancellationToken.None);
        var item = Guid.TryParse(itemKey, out var itemId) ? items?.SingleOrDefault(x => x.ShopItemId == itemId) : null;
        if (item is null || !item.IsAvailable) { await ModifyOriginalResponseAsync(p => p.Content = "Select a shop item you are allowed to purchase from the dropdown."); return; }
        if (quantity > item.RemainingQuantity)
        {
            await ModifyOriginalResponseAsync(p => p.Content = $"You can purchase at most **{item.RemainingQuantity}** of **{item.Name}**.");
            return;
        }
        var purchase = await api.PurchaseAsync(Context.User.Id.ToString(), new ShopPurchaseInput(item.ShopItemId, quantity), CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = purchase is null ? "The purchase could not be completed. Check your DKP, item limit and achievement requirements." : $"Purchase completed: **{purchase.Quantity} x {purchase.ItemName}** for **{purchase.TotalDkpCost} DKP**.");
    }
}

public sealed class PurchasesModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("purchases", "Show your DKP shop purchases", runMode: RunMode.Async)]
    public async Task ListAsync(
        [Summary("status", "Filter purchases by status")]
        [Choice("All", "all")]
        [Choice("Active", "active")]
        [Choice("Cancelled", "cancelled")]
        [Choice("Used", "used")] string status = "all")
    {
        await DeferAsync(ephemeral: true);
        status = status.Trim().ToLowerInvariant();
        if (status is not ("all" or "active" or "cancelled" or "used")) { await ModifyOriginalResponseAsync(p => p.Content = "Status must be all, active, cancelled or used."); return; }
        var purchases = await api.GetPurchasesAsync(Context.User.Id.ToString(), status, CancellationToken.None);
        if (purchases is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your purchases could not be loaded."); return; }
        if (purchases.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "You have no shop purchases."); return; }
        var output = new StringBuilder("**Your shop purchases**\n");
        foreach (var purchase in purchases)
        {
            output.AppendLine($"**{purchase.Quantity} x {purchase.ItemName}**, {purchase.TotalDkpCost} DKP, {purchase.Status}");
        }
        output.AppendLine("\nUse `/purchases-cancel` and select an active purchase to receive a refund.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("purchases-cancel", "Cancel one of your active shop purchases", runMode: RunMode.Async)]
    public async Task CancelAsync([Summary("purchase", "Select an active purchase")] [Autocomplete<PurchaseAutocompleteHandler>] string purchaseId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(purchaseId, out var id)) { await ModifyOriginalResponseAsync(p => p.Content = "The purchase ID is not valid."); return; }
        var cancelled = await api.CancelPurchaseAsync(Context.User.Id.ToString(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = cancelled ? "Purchase cancelled and DKP refunded." : "The purchase could not be cancelled. It may already be cancelled or unavailable.");
    }
}
