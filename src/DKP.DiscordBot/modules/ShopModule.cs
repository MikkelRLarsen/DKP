using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

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
            var limits = new List<string> { $"{item.RemainingQuantity} available of {item.MaxPerUser}" };
            if (item.MaxActivePerUser is int active) limits.Add($"active max {active}");
            if (item.MaxPerDay is int daily) limits.Add($"daily max {daily}");
            if (item.MaxPerWeek is int weekly) limits.Add($"weekly max {weekly}");
            if (item.MaxPerMonth is int monthly) limits.Add($"monthly max {monthly}");
            output.AppendLine($"**{item.Name}**: {item.Price} DKP each, {string.Join(", ", limits)}{(item.IsAvailable ? string.Empty : " (unavailable)")}{requirements}");
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

    [ComponentInteraction("shop:buy:*", true)]
    public async Task BuyButtonAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var id))
        {
            await RespondAsync("This shop item button is invalid.", ephemeral: true);
            return;
        }

        try
        {
            var items = await api.GetShopAvailabilityAsync(Context.User.Id.ToString(), CancellationToken.None);
            var item = items?.SingleOrDefault(x => x.ShopItemId == id);
            if (item is null || !item.IsAvailable || item.RemainingQuantity <= 0)
            {
                await RespondAsync("This shop item is not available for you. Check your DKP, achievement requirements and purchase limit.", ephemeral: true);
                return;
            }

            var maximum = Math.Min(item.RemainingQuantity, 25);
            var menu = new SelectMenuBuilder()
                .WithCustomId($"shop:quantity:{id:D}")
                .WithPlaceholder("Select quantity")
                .WithMinValues(1)
                .WithMaxValues(1);
            for (var quantity = 1; quantity <= maximum; quantity++)
                menu.AddOption(quantity.ToString(), quantity.ToString(), $"Buy {quantity} x {item.Name}");

            await RespondAsync($"Select quantity for **{item.Name}** (up to {item.RemainingQuantity}).", components: new ComponentBuilder().WithSelectMenu(menu).Build(), ephemeral: true);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Shop button failed for {id}: {exception.Message}");
            await RespondAsync("The shop item could not be loaded. Please use `/shop buy` instead.", ephemeral: true);
        }
    }

    [ComponentInteraction("shop:quantity:*", true)]
    public async Task BuyQuantityAsync(string itemId)
    {
        var component = Context.Interaction as SocketMessageComponent;
        var value = component?.Data.Values.FirstOrDefault();
        if (!Guid.TryParse(itemId, out var id) || !int.TryParse(value, out var quantity) || quantity <= 0)
        {
            await RespondAsync("Select a valid quantity and try again.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var purchase = await api.PurchaseAsync(Context.User.Id.ToString(), new ShopPurchaseInput(id, quantity), CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = purchase is null
                ? "The purchase could not be completed. Check your DKP, item limit and achievement requirements."
                : $"Purchase completed: **{purchase.Quantity} x {purchase.ItemName}** for **{purchase.TotalDkpCost} DKP**.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Shop quantity selection failed for {id}: {exception.Message}");
            await ModifyOriginalResponseAsync(p => p.Content = "The purchase could not be completed. Please try again.");
        }
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
