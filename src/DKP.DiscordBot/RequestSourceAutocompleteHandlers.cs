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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        BotAchievementOverviewDto? overview;
        try { overview = await api.GetAchievementsAsync(context.User.Id.ToString(), timeout.Token); }
        catch (Exception) { return AutocompletionResult.FromSuccess([]); }
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

public sealed class AchievementRequestAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var overview = await api.GetAchievementsAsync(context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) return AutocompletionResult.FromSuccess([]);
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = overview.Requests
            .Where(x => x.AchievementId is not null && x.IsPending)
            .Where(x => string.IsNullOrWhiteSpace(input) || x.PresetName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.PresetName} ({x.Amount} DKP)"), x.Id.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}

public sealed class CharacterAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var characters = await api.GetCharactersAsync(context.User.Id.ToString(), CancellationToken.None);
        if (characters is null) return AutocompletionResult.FromSuccess([]);
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = characters
            .Where(x => string.IsNullOrWhiteSpace(input) || $"{x.FirstName} {x.LastName}".Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{(x.IsMain ? "⭐ " : string.Empty)}{x.FirstName} {x.LastName}"), x.Id.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}

public sealed class ShopItemAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var items = await api.GetShopAvailabilityAsync(context.User.Id.ToString(), CancellationToken.None);
        if (items is null) return AutocompletionResult.FromSuccess([]);
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = items
            .Where(x => x.IsAvailable)
            .Where(x => string.IsNullOrWhiteSpace(input) || x.Name.Contains(input, StringComparison.OrdinalIgnoreCase) || x.Key.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.Name} ({x.Price} DKP, {x.RemainingQuantity} available)"), x.ShopItemId.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}

public sealed class PurchaseAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var purchases = await api.GetPurchasesAsync(context.User.Id.ToString(), "active", CancellationToken.None);
        if (purchases is null) return AutocompletionResult.FromSuccess([]);
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = purchases
            .Where(x => string.IsNullOrWhiteSpace(input) || x.ItemName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.Quantity} x {x.ItemName} ({x.TotalDkpCost} DKP)"), x.Id.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}

public sealed class DkpRequestAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction, IParameterInfo parameter, IServiceProvider services)
    {
        var api = services.GetRequiredService<DkpApiClient>();
        var overview = await api.GetAchievementsAsync(context.User.Id.ToString(), CancellationToken.None);
        if (overview is null) return AutocompletionResult.FromSuccess([]);
        var input = interaction.Data.Current.Value?.ToString() ?? string.Empty;
        var suggestions = overview.Requests
            .Where(x => x.PresetId is not null && x.IsPending)
            .Where(x => string.IsNullOrWhiteSpace(input) || x.PresetName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(x => new AutocompleteResult(Shorten($"{x.PresetName} x{x.Quantity} ({x.Amount} DKP)"), x.Id.ToString("D")))
            .ToArray();
        return AutocompletionResult.FromSuccess(suggestions);
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value[..97] + "...";
}
