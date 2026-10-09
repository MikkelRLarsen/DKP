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
            output.AppendLine($"• **{definition.Name}** (`{definition.Key}`) — {definition.DkpAmount} DKP — {status}");
            if (!string.IsNullOrWhiteSpace(definition.Description)) output.AppendLine($"  {definition.Description}");
        }
        if (overview.Definitions.Count == 0) output.AppendLine("No active achievements are currently available.");
        output.AppendLine("\nUse `/achievement request` to request an available or previously revoked achievement.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("achievement-requests", "Show your achievement and DKP requests", runMode: RunMode.Async)]
    public async Task RequestsAsync()
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your requests could not be loaded."); return; }
        var requests = overview.Requests;
        if (requests.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "You have no requests."); return; }
        var output = new StringBuilder("**Your requests**\n");
        foreach (var request in requests)
            output.AppendLine($"`{request.Id}` — **{request.PresetName}**, {request.Amount} DKP, {request.StatusName}");
        output.AppendLine("\nUse `/achievement cancel` with a pending request ID to cancel it.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }
}

[Group("achievement", "Request an achievement DKP award")]
public sealed class AchievementRequestModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("requests", "Show your achievement and DKP requests", runMode: RunMode.Async)]
    public async Task RequestsAsync()
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) { await ModifyOriginalResponseAsync(p => p.Content = "Your requests could not be loaded."); return; }
        if (overview.Requests.Count == 0) { await ModifyOriginalResponseAsync(p => p.Content = "You have no requests."); return; }
        var output = new StringBuilder("**Your requests**\n");
        foreach (var request in overview.Requests)
            output.AppendLine($"`{request.Id}` — **{request.PresetName}**, {request.Amount} DKP, {request.StatusName}");
        output.AppendLine("\nUse `/achievement cancel` with a pending request ID to cancel it.");
        await ModifyOriginalResponseAsync(p => p.Content = output.ToString());
    }

    [SlashCommand("request", "Request an available achievement", runMode: RunMode.Async)]
    public async Task RequestAsync([Summary("achievement", "The achievement key shown by /achievements")] string achievement, [Summary("comment", "Optional comment for the Officer")] string? comment = null)
    {
        await DeferAsync(ephemeral: true);
        var overview = await api.GetAchievementsAsync(Context.User.Id.ToString(), CancellationToken.None);
        var definition = overview?.Definitions.SingleOrDefault(x => string.Equals(x.Key, achievement.Trim(), StringComparison.OrdinalIgnoreCase));
        if (definition is null) { await ModifyOriginalResponseAsync(p => p.Content = "That achievement was not found. Use `/achievements` to see valid keys."); return; }
        if (overview!.UserAchievements.Any(x => x.AchievementId == definition.Id && x.IsActive)) { await ModifyOriginalResponseAsync(p => p.Content = "You already have this achievement."); return; }
        if (overview.Requests.Any(x => x.AchievementId == definition.Id && x.IsPending)) { await ModifyOriginalResponseAsync(p => p.Content = "You already have a pending request for this achievement."); return; }
        var request = await api.RequestAchievementAsync(Context.User.Id.ToString(), definition.Id, comment, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = request is null ? "The achievement request could not be created." : $"Request submitted for **{definition.Name}**. An Officer must approve it before the DKP is awarded.");
    }

    [SlashCommand("cancel", "Cancel one of your pending requests", runMode: RunMode.Async)]
    public async Task CancelAsync([Summary("request_id", "The request ID shown by /achievement-requests")] string requestId)
    {
        await DeferAsync(ephemeral: true);
        if (!Guid.TryParse(requestId, out var id)) { await ModifyOriginalResponseAsync(p => p.Content = "The request ID is not valid."); return; }
        var cancelled = await api.CancelAchievementRequestAsync(Context.User.Id.ToString(), id, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = cancelled ? "Request cancelled." : "The request could not be cancelled. It may already be processed.");
    }
}
