using System.Text;
using Discord.Interactions;

namespace DKP.DiscordBot;

public sealed class AchievementModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("achievements", "Show your achievements and available achievement awards", runMode: RunMode.Async)]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your achievements could not be loaded."); return; }
        var output = new StringBuilder("**Achievements**\n");
        foreach (var definition in overview.Definitions)
        {
            var obtained = overview.UserAchievements.Any(x => x.AchievementId == definition.Id && x.IsActive);
            var pending = overview.Requests.Any(x => x.AchievementId == definition.Id && x.IsPending);
            var previouslyRevoked = overview.UserAchievements.Any(x => x.AchievementId == definition.Id && !x.IsActive);
            var status = obtained ? "Obtained" : pending ? "Request pending" : previouslyRevoked ? "Previously revoked" : "Available";
            output.AppendLine($"• **{definition.Name}** — {definition.DkpAmount} DKP — {status}");
            if (!string.IsNullOrWhiteSpace(definition.Description)) output.AppendLine($"  {definition.Description}");
        }
        if (overview.Definitions.Count == 0) output.AppendLine("No active achievements are currently available.");
        output.AppendLine("\nUse `/achievement request` and select an available or previously revoked achievement from the dropdown.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

}

[Group("achievement", "Request an achievement DKP award")]
public sealed class AchievementRequestModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("request-many", "Request an achievement for yourself and tagged members", runMode: RunMode.Async)]
    public async Task RequestManyAsync(
        [Summary("achievement", "Select an available achievement")] [Autocomplete<AchievementAutocompleteHandler>] string achievement,
        [Summary("users", "Space-separated Discord mentions, for example @UserA @UserB")] string users,
        [Summary("comment", "Optional comment for the Officer")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        var definition = Guid.TryParse(achievement, out var achievementId) ? overview?.Definitions.SingleOrDefault(x => x.Id == achievementId) : null;
        if (definition is null) { await ModifyOriginalResponseAsync(p => p.Content = "Select an available achievement from the dropdown."); return; }
        var targetIds = ExtractDiscordIds(users);
        if (targetIds.Length == 0)
        {
            await ModifyOriginalResponseAsync(p => p.Content = "Tag at least one Discord member, for example `@UserA @UserB`.");
            return;
        }
        var requests = await api.RequestAchievementForUsersAsync(Context.User.Id.ToString(), definition.Id, targetIds, comment, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = requests is null
            ? "The multi-user request could not be created. No requests were saved; check that every member is eligible."
            : $"Created **{requests.Count}** pending request(s) for **{definition.Name}**. An Officer must approve them before DKP is awarded.");
    }

    private static string[] ExtractDiscordIds(string value)
        => (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().TrimStart('<').TrimEnd('>'))
            .Select(token => token.StartsWith("@!", StringComparison.Ordinal) ? token[2..] : token.StartsWith("@", StringComparison.Ordinal) ? token[1..] : token)
            .Where(token => ulong.TryParse(token, out _))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    [SlashCommand("requests", "Show your achievement and DKP requests", runMode: RunMode.Async)]
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
        var requests = overview.Requests
            .Where(x => x.AchievementId is not null)
            .Where(x => status == "all" || x.StatusName.Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (requests.Length == 0) { await ModifyOriginalResponseAsync(p => p.Content = "No requests match that status."); return; }
        var output = new StringBuilder("**Your requests**\n");
        foreach (var request in requests)
            output.AppendLine($"**{request.PresetName}**, {request.Amount} DKP, {request.StatusName}");
        output.AppendLine("\nUse `/achievement cancel` and select a pending request from the dropdown to cancel it.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("request", "Request an available achievement", runMode: RunMode.Async)]
    public async Task RequestAsync([Summary("achievement", "Select an available achievement")] [Autocomplete<AchievementAutocompleteHandler>] string achievement, [Summary("comment", "Optional comment for the Officer")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), timeout.Token);
            var definition = Guid.TryParse(achievement, out var achievementId) ? overview?.Definitions.SingleOrDefault(x => x.Id == achievementId) : null;
            if (definition is null) { await ModifyOriginalResponseAsync(p => p.Content = "Select an available achievement from the dropdown."); return; }
            if (overview!.UserAchievements.Any(x => x.AchievementId == definition.Id && x.IsActive)) { await ModifyOriginalResponseAsync(p => p.Content = "You already have this achievement."); return; }
            if (overview.Requests.Any(x => x.AchievementId == definition.Id && x.IsPending)) { await ModifyOriginalResponseAsync(p => p.Content = "You already have a pending request for this achievement."); return; }
            var request = await api.RequestAchievementAsync(Context.User.Id.ToString(), definition.Id, comment, timeout.Token);
            await ModifyOriginalResponseAsync(p => p.Content = request is null ? "The achievement request could not be created." : $"Request submitted for **{definition.Name}**. An Officer must approve it before the DKP is awarded.");
        }
        catch (OperationCanceledException)
        {
            await ModifyOriginalResponseAsync(p => p.Content = "The achievement request timed out. Please try again.");
        }
        catch (Exception)
        {
            await ModifyOriginalResponseAsync(p => p.Content = "The achievement request could not be completed. Please try again.");
        }
    }

    [SlashCommand("cancel", "Cancel one of your pending requests", runMode: RunMode.Async)]
    public async Task CancelAsync([Summary("request", "Select a pending achievement request")] [Autocomplete<AchievementRequestAutocompleteHandler>] string requestId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(requestId, out var id)) { await ModifyOriginalResponseAsync(p => p.Content = "Select a request from the dropdown."); return; }
        var cancelled = await api.CancelAchievementRequestAsync(Context.User.Id.ToString(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = cancelled ? "Request cancelled." : "The request could not be cancelled. It may already be processed.");
    }
}
