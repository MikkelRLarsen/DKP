using Discord.Interactions;

namespace DKP.DiscordBot.modules;

public sealed class DkpPingModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("dkp-ping", "Checks the DKP API connection", runMode: RunMode.Async)]
    public async Task PingAsync()
    {
        await DeferAsync(ephemeral: true);
        var health = await api.GetHealthAsync(CancellationToken.None);
        if (health is null)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = "The DKP API is currently unavailable.");
            return;
        }
        await ModifyOriginalResponseAsync(properties => properties.Content = $"DKP API is online ({health.Service}).");
    }
}
