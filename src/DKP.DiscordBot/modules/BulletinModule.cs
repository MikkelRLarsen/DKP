using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot.modules;

[Group("bulletin", "View and claim Bulletin Board items")]
public sealed class BulletinModule(DkpApiClient api, ILogger<BulletinModule> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("claim", "Claim a Bulletin Board item", runMode: RunMode.Async)]
    public async Task ClaimAsync(
        [Summary("item", "Select a Bulletin Board item")]
        [Autocomplete<BulletinItemAutocompleteHandler>] string itemId,
        [Summary("quantity", "How many quantities to claim")] int quantity = 1)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(itemId, out var id))
        {
            await ModifyOriginalResponseAsync(p => p.Content = "Select a Bulletin Board item from the dropdown.");
            return;
        }

        try
        {
            var claim = await api.ClaimBulletinAsync(Context.User.Id.ToString(), id, quantity, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = claim is null
                ? "The claim could not be created. The item may be expired, full or already claimed."
                : $"Claim submitted for **{claim.BulletinTitle}** (quantity {claim.Quantity}). An Officer must approve it.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin slash-command claim failed for item {ItemId}.", id);
            await ModifyOriginalResponseAsync(p => p.Content = "The claim could not be completed. Please try again.");
        }
    }

    [SlashCommand("approve", "Approve a pending Bulletin Board claim", runMode: RunMode.Async)]
    public async Task ApproveAsync(
        [Summary("claim", "Select a pending Bulletin claim")]
        [Autocomplete<BulletinClaimAutocompleteHandler>] string claimId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(claimId, out var id))
        {
            await ModifyOriginalResponseAsync(p => p.Content = "Select a pending Bulletin claim from the dropdown.");
            return;
        }

        try
        {
            var approved = await api.ApproveBulletinClaimAsync(Context.User.Id.ToString(), id, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = approved
                ? "Bulletin claim approved."
                : "The claim could not be approved. It may already be processed or you may not be an Officer.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin approve command failed for claim {ClaimId}.", id);
            await ModifyOriginalResponseAsync(p => p.Content = "The claim could not be approved. Please try again.");
        }
    }

    [ComponentInteraction("bulletin:approve:*", true)]
    public async Task ApproveButtonAsync(string claimId)
    {
        if (!Guid.TryParse(claimId, out var id))
        {
            await RespondAsync("This Bulletin claim button is invalid.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var approved = await api.ApproveBulletinClaimAsync(Context.User.Id.ToString(), id, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = approved
                ? "Bulletin claim approved."
                : "The claim could not be approved. It may already be processed or you may not be an Officer.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin approve button failed for claim {ClaimId}.", id);
            await ModifyOriginalResponseAsync(p => p.Content = "The claim could not be approved. Please try again.");
        }
    }

    [ComponentInteraction("bulletin:review:*", true)]
    public async Task ReviewButtonAsync(string claimId)
    {
        if (!Guid.TryParse(claimId, out var id))
        {
            await RespondAsync("This Bulletin claim button is invalid.", ephemeral: true);
            return;
        }

        var menu = new SelectMenuBuilder()
            .WithCustomId($"bulletin:review-action:{id:D}")
            .WithPlaceholder("Choose an action")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("Approve", "approve", "Award the DKP and approve the claim")
            .AddOption("Deny", "deny", "Reject the claim without awarding DKP");
        await RespondAsync("Choose what to do with this Bulletin claim:", components: new ComponentBuilder().WithSelectMenu(menu).Build(), ephemeral: true);
    }

    [ComponentInteraction("bulletin:review-action:*", true)]
    public async Task ReviewActionAsync(string claimId)
    {
        var component = Context.Interaction as SocketMessageComponent;
        var action = component?.Data.Values.FirstOrDefault();
        if (!Guid.TryParse(claimId, out var id) || action is not ("approve" or "deny"))
        {
            await RespondAsync("Select a valid review action and try again.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var succeeded = action == "approve"
                ? await api.ApproveBulletinClaimAsync(Context.User.Id.ToString(), id, CancellationToken.None)
                : await api.DenyBulletinClaimAsync(Context.User.Id.ToString(), id, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = succeeded
                ? $"Bulletin claim {(action == "approve" ? "approved" : "denied")}."
                : "The claim could not be processed. It may already be processed or you may not be an Officer.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin claim review failed for claim {ClaimId}.", id);
            await ModifyOriginalResponseAsync(p => p.Content = "The claim could not be processed. Please try again.");
        }
    }

    [ComponentInteraction("bulletin:claim:*", true)]
    public async Task ClaimButtonAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var id))
        {
            await RespondAsync("This Bulletin Board button is invalid.", ephemeral: true);
            return;
        }

        try
        {
            var items = await api.GetBulletinItemsAsync(Context.User.Id.ToString(), CancellationToken.None);
            var item = items?.SingleOrDefault(x => x.Id == id);
            if (item is null || item.RemainingQuantity <= 0)
            {
                await RespondAsync("This Bulletin Board item is no longer available.", ephemeral: true);
                return;
            }

            var maximum = item.UniquePerUser ? 1 : Math.Min(item.RemainingQuantity, 25);
            var menu = new SelectMenuBuilder()
                .WithCustomId($"bulletin:quantity:{id:D}")
                .WithPlaceholder(item.UniquePerUser ? "Only once per user" : "Select quantity")
                .WithMinValues(1)
                .WithMaxValues(1);
            for (var quantity = 1; quantity <= maximum; quantity++)
                menu.AddOption(quantity.ToString(), quantity.ToString(), $"Claim {quantity} from {item.Title}");

            await RespondAsync($"Select quantity for **{item.Title}** (up to {item.RemainingQuantity}).", components: new ComponentBuilder().WithSelectMenu(menu).Build(), ephemeral: true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin button failed for item {ItemId}.", id);
            await RespondAsync("The Bulletin Board item could not be loaded. Please use `/bulletin claim` instead.", ephemeral: true);
        }
    }

    [ComponentInteraction("bulletin:quantity:*", true)]
    public async Task ClaimQuantityAsync(string itemId)
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
            var claim = await api.ClaimBulletinAsync(Context.User.Id.ToString(), id, quantity, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = claim is null
                ? "The claim could not be created. The item may be expired, full or already claimed."
                : $"Claim submitted for **{claim.BulletinTitle}** (quantity {claim.Quantity}). An Officer must approve it.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bulletin quantity selection failed for item {ItemId}.", id);
            await ModifyOriginalResponseAsync(p => p.Content = "The claim could not be completed. Please try again.");
        }
    }
}
