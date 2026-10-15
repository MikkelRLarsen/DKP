using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot.modules;

[Group("dkp", "View your DKP and request awards")]
public sealed class DkpModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("balance", "Show your current DKP balance", runMode: RunMode.Async)]
    public async Task BalanceAsync()
    {
        await DeferAsync(ephemeral: true);
        var history = await api.GetDkpAsync(Context.User.Id.ToString(), CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = history is null ? "Your DKP account could not be loaded." : $"Your current DKP balance is **{history.Balance.Amount}**.");
    }

    [SlashCommand("history", "Show your recent DKP history", runMode: RunMode.Async)]
    public async Task HistoryAsync()
    {
        await DeferAsync(ephemeral: true);
        var history = await api.GetDkpHistoryAsync(Context.User.Id.ToString(), 10, CancellationToken.None);
        if (history is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your DKP history could not be loaded."); return; }
        if (history.Transactions.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = $"Your current DKP balance is **{history.Balance.Amount}**.\nNo DKP history found."); return; }
        var output = new StringBuilder($"Current balance: **{history.Balance.Amount}**\n");
        foreach (var transaction in history.Transactions)
        {
            var sign = transaction.Amount >= 0 ? "+" : string.Empty;
            output.AppendLine($"`{transaction.CreatedAtUtc:yyyy-MM-dd}` **{sign}{transaction.Amount}** — {transaction.Reason} ({transaction.CreatedByDiscordName})");
        }
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("sources", "Show available DKP preset sources", runMode: RunMode.Async)]
    public async Task SourcesAsync()
    {
        await DeferAsync(ephemeral: true);
        var sources = await api.GetDkpSourcesAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (sources is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your DKP sources could not be loaded."); return; }
        if (sources.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "You have no remaining DKP sources."); return; }
        var output = new StringBuilder("**Available DKP sources**\n");
        foreach (var source in sources) output.AppendLine($"`{source.Name}` — **+{source.Amount} DKP**, {source.Remaining} remaining — {source.Reason}");
        output.AppendLine("\nUse `/dkp request` and select a preset from the dropdown.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("request", "Request a DKP award from a preset", runMode: RunMode.Async)]
    public async Task RequestAsync([Summary("preset", "Select an available DKP preset")] [Autocomplete<DkpPresetAutocompleteHandler>] string preset, [Summary("quantity", "Number of applications requested")] int quantity = 1, [Summary("comment", "Optional comment for the Officer")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        var sources = await api.GetDkpSourcesAsync(Context.User.Id.ToString(), CancellationToken.None);
        var source = Guid.TryParse(preset, out var presetId) ? sources?.SingleOrDefault(x => x.PresetId == presetId) : null;
        if (source is null) { await ModifyOriginalResponseAsync(p => p.Content = "Select an available preset from the dropdown. It may no longer be available."); return; }
        var result = await api.RequestDkpAsync(Context.User.Id.ToString(), source.PresetId, quantity, [], comment, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = result is null ? "The DKP request could not be created." : $"Created {result.Count} pending DKP request(s) for **{source.Name}**.");
    }

    [SlashCommand("request-many", "Request a DKP preset award for tagged members", runMode: RunMode.Async)]
    public async Task RequestManyAsync([Summary("preset", "Select an available DKP preset")] [Autocomplete<DkpPresetAutocompleteHandler>] string preset, [Summary("users", "Space-separated Discord mentions, for example @UserA @UserB")] string users, [Summary("quantity", "Number of applications requested per user")] int quantity = 1, [Summary("comment", "Optional comment for the Officer")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        var sources = await api.GetDkpSourcesAsync(Context.User.Id.ToString(), CancellationToken.None);
        var source = Guid.TryParse(preset, out var presetId) ? sources?.SingleOrDefault(x => x.PresetId == presetId) : null;
        if (source is null) { await ModifyOriginalResponseAsync(p => p.Content = "Select an available preset from the dropdown. It may no longer be available."); return; }
        var targetIds = ExtractDiscordIds(users);
        if (targetIds.Length == 0) { await ModifyOriginalResponseAsync(p => p.Content = "Tag at least one Discord member, for example `@UserA @UserB`."); return; }
        var result = await api.RequestDkpAsync(Context.User.Id.ToString(), source.PresetId, quantity, targetIds, comment, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = result is null ? "The multi-user DKP request could not be created. No requests were saved." : $"Created **{result.Count}** pending DKP request(s) for **{source.Name}**.");
    }

    [SlashCommand("requests", "Show your DKP requests", runMode: RunMode.Async)]
    public async Task RequestsAsync(
        [Summary("status", "Filter: all, pending, approved, rejected or cancelled")]
        [Choice("All", "all")]
        [Choice("Pending", "pending")]
        [Choice("Approved", "approved")]
        [Choice("Rejected", "rejected")]
        [Choice("Cancelled", "cancelled")] string status = "all")
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your requests could not be loaded."); return; }
        status = status.Trim().ToLowerInvariant();
        if (status is not ("all" or "pending" or "approved" or "rejected" or "cancelled")) { await ModifyOriginalResponseAsync(p => p.Content = "Status must be all, pending, approved, rejected or cancelled."); return; }
        var requests = overview.Requests.Where(x => x.PresetId is not null && (status == "all" || x.StatusName.Equals(status, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (requests.Length == 0) { await ModifyOriginalResponseAsync(p => p.Content = "No DKP requests match that status."); return; }
        var output = new StringBuilder("**Your DKP requests**\n");
        foreach (var request in requests) output.AppendLine($"**{request.PresetName}**, x{request.Quantity}, {request.Amount} DKP, {request.StatusName}");
        output.AppendLine("\nUse `/dkp cancel` and select a pending request from the dropdown to cancel it.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("cancel", "Cancel one of your pending DKP requests", runMode: RunMode.Async)]
    public async Task CancelAsync([Summary("request", "Select a pending DKP request")] [Autocomplete<DkpRequestAutocompleteHandler>] string requestId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(requestId, out var id)) { await ModifyOriginalResponseAsync(p => p.Content = "The request ID is not valid."); return; }
        var cancelled = await api.CancelDkpRequestAsync(Context.User.Id.ToString(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = cancelled ? "DKP request cancelled." : "The request could not be cancelled. It may already be processed.");
    }

    [SlashCommand("review", "Approve or deny a pending DKP or achievement request", runMode: RunMode.Async)]
    public async Task ReviewAsync(
        [Summary("request", "Select a pending request")]
        [Autocomplete<BotAwardRequestAutocompleteHandler>] string requestId,
        [Summary("action", "Approve or deny the request")]
        [Choice("Approve", "approve")]
        [Choice("Deny", "deny")] string action,
        [Summary("comment", "Optional review comment")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(requestId, out var id) || action is not ("approve" or "deny"))
        {
            await ModifyOriginalResponseAsync(p => p.Content = "Select a valid pending request and action.");
            return;
        }

        var success = action == "approve"
            ? await api.ApproveAwardRequestAsync(Context.User.Id.ToString(), id, comment, CancellationToken.None)
            : await api.RejectAwardRequestAsync(Context.User.Id.ToString(), id, comment, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = success
            ? $"Request {action}d successfully."
            : "The request could not be processed. It may already be handled or you may not be an Officer.");
    }

    [ComponentInteraction("award:review:*", true)]
    public async Task ReviewButtonAsync(string requestId)
    {
        if (!Guid.TryParse(requestId, out var id))
        {
            await RespondAsync("This request button is invalid.", ephemeral: true);
            return;
        }

        var menu = new SelectMenuBuilder()
            .WithCustomId($"award:review-action:{id:D}")
            .WithPlaceholder("Approve or deny")
            .WithMinValues(1).WithMaxValues(1)
            .AddOption("Approve", "approve", "Award the requested DKP")
            .AddOption("Deny", "deny", "Reject without awarding DKP");
        await RespondAsync("Choose an action for this request:", components: new ComponentBuilder().WithSelectMenu(menu).Build(), ephemeral: true);
    }

    [ComponentInteraction("award:review-action:*", true)]
    public async Task ReviewActionAsync(string requestId)
    {
        var component = Context.Interaction as SocketMessageComponent;
        var action = component?.Data.Values.FirstOrDefault();
        if (!Guid.TryParse(requestId, out var id) || action is not ("approve" or "deny"))
        {
            await RespondAsync("Select a valid review action and try again.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var success = action == "approve"
            ? await api.ApproveAwardRequestAsync(Context.User.Id.ToString(), id, null, CancellationToken.None)
            : await api.RejectAwardRequestAsync(Context.User.Id.ToString(), id, null, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = success
            ? $"Request {action}d successfully."
            : "The request could not be processed. It may already be handled or you may not be an Officer.");
    }

    [ComponentInteraction("source:request:dkp:*", true)]
    public async Task RequestSourceButtonAsync(string presetId)
    {
        if (!Guid.TryParse(presetId, out var id)) { await RespondAsync("This DKP source button is invalid.", ephemeral: true); return; }
        try
        {
            var sources = await api.GetDkpSourcesAsync(Context.User.Id.ToString(), CancellationToken.None);
            var source = sources?.SingleOrDefault(x => x.PresetId == id);
            if (source is null || source.Remaining <= 0)
            {
                await RespondAsync("This DKP source has no remaining applications.", ephemeral: true);
                return;
            }

            var maximum = Math.Min(source.Remaining, 25);
            var menu = new SelectMenuBuilder()
                .WithCustomId($"source:dkp-quantity:{id:D}")
                .WithPlaceholder("Select quantity")
                .WithMinValues(1)
                .WithMaxValues(1);
            for (var quantity = 1; quantity <= maximum; quantity++)
                menu.AddOption(quantity.ToString(), quantity.ToString(), $"Request {quantity} × {source.Name}");

            await RespondAsync($"Select how many applications to request for **{source.Name}** (up to {source.Remaining}).", components: new ComponentBuilder().WithSelectMenu(menu).Build(), ephemeral: true);
        }
        catch (Exception)
        {
            await RespondAsync("The DKP source could not be loaded. Please use `/dkp request` instead.", ephemeral: true);
        }
    }

    [ComponentInteraction("source:dkp-quantity:*", true)]
    public async Task RequestSourceQuantityAsync(string presetId)
    {
        var component = Context.Interaction as SocketMessageComponent;
        var value = component?.Data.Values.FirstOrDefault();
        if (!Guid.TryParse(presetId, out var id) || !int.TryParse(value, out var quantity) || quantity <= 0)
        {
            await RespondAsync("Select a valid quantity and try again.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var sources = await api.GetDkpSourcesAsync(Context.User.Id.ToString(), CancellationToken.None);
            var source = sources?.SingleOrDefault(x => x.PresetId == id);
            if (source is null || quantity > source.Remaining)
            {
                await ModifyOriginalResponseAsync(p => p.Content = "The selected quantity is no longer available. Please open the source again.");
                return;
            }

            var result = await api.RequestDkpAsync(Context.User.Id.ToString(), id, quantity, [], null, CancellationToken.None);
            await ModifyOriginalResponseAsync(p => p.Content = result is null
                ? "The DKP request could not be created. Please try again."
                : $"Created **{quantity}** pending DKP request(s) for **{source.Name}**. An Officer must approve them.");
        }
        catch (Exception)
        {
            await ModifyOriginalResponseAsync(p => p.Content = "The DKP request could not be completed. Please try again.");
        }
    }

    private static string[] ExtractDiscordIds(string value)
        => (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().TrimStart('<').TrimEnd('>'))
            .Select(token => token.StartsWith("@!", StringComparison.Ordinal) ? token[2..] : token.StartsWith("@", StringComparison.Ordinal) ? token[1..] : token)
            .Where(token => ulong.TryParse(token, out _)).Distinct(StringComparer.Ordinal).ToArray();
}
