using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace DKP.DiscordBot;

public sealed class DkpPresetAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var sources = await api.GetDkpSourcesAsync(context.User.Id.ToString(), CancellationToken.None);
        if (sources is null) return AutocompletionResult.FromSuccess([]);

        var input = autocompleteInteraction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = sources
            .Where(x => string.IsNullOrWhiteSpace(input) || x.Name.Contains(input, StringComparison.OrdinalIgnoreCase) || x.Reason.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.Name} (+{x.Amount} DKP, {x.Remaining} left)"), x.PresetId.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}

public sealed class AchievementAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var overview = await api.GetAchievementsAsync(context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) return AutocompletionResult.FromSuccess([]);

        var input = autocompleteInteraction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = overview.Definitions
            .Where(x => !overview.UserAchievements.Any(a => a.AchievementId == x.Id && a.IsActive))
            .Where(x => !overview.Requests.Any(r => r.AchievementId == x.Id && r.IsPending))
            .Where(x => string.IsNullOrWhiteSpace(input) || x.Name.Contains(input, StringComparison.OrdinalIgnoreCase) || x.Key.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.Name} (+{x.DkpAmount} DKP)"), x.Id.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}
