using Discord.Interactions;

namespace DKP.DiscordBot.modules;

 [Group("account", "Manage your DKP account")]
public sealed class AccountModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create or update your DKP account", runMode: RunMode.Async)]
    public async Task CreateAsync()
    {
        await DeferAsync(ephemeral: true);
        var input = new BotAccountInput(Context.User.Username, Context.User.GetAvatarUrl());
        var account = await api.CreateAccountAsync(Context.User.Id.ToString(), input, CancellationToken.None);
        await ModifyOriginalResponseAsync(p => p.Content = account is null
            ? "Your DKP account could not be created."
            : account.Created
                ? "Your DKP account was created. You can now use the DKP bot features."
                : "Your DKP account already exists and was updated with your current Discord profile.");
    }
}
