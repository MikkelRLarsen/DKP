using Discord.Interactions;

namespace DKP.DiscordBot.modules;

[Group("notifications", "Manage your private DKP bot notifications")]
public sealed class NotificationModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("toggle", "Toggle private review notifications on or off", runMode: RunMode.Async)]
    public async Task ToggleAsync()
    {
        await DeferAsync(ephemeral: true);
        var preference = await api.ToggleReviewNotificationsAsync(Context.User.Id.ToString(), CancellationToken.None);
        await ModifyOriginalResponseAsync(properties => properties.Content = preference is null
            ? "Your review notification setting could not be changed. Make sure your DKP account exists."
            : preference.Enabled
                ? "Private review notifications are now **enabled**."
                : "Private review notifications are now **disabled**.");
    }
}
