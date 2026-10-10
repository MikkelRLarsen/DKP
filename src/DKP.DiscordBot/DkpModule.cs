using System.Text;
using Discord.Interactions;

namespace DKP.DiscordBot;

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

    private static string[] ExtractDiscordIds(string value)
        => (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().TrimStart('<').TrimEnd('>'))
            .Select(token => token.StartsWith("@!", StringComparison.Ordinal) ? token[2..] : token.StartsWith("@", StringComparison.Ordinal) ? token[1..] : token)
            .Where(token => ulong.TryParse(token, out _)).Distinct(StringComparer.Ordinal).ToArray();
}
