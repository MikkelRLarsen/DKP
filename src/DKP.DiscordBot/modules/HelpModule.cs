using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot;

public sealed class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string HelpMessage = """
        🤖 **DKP Bot Help**

        The bot gives guild members access to common DKP features directly in Discord. You can manage characters, view DKP, request repeatable DKP and achievements, claim Bulletin Board items, buy shop items and view your purchases.

        **Getting started**
        1. Open <https://wowforever.coffeecottage.dk/> and log in with Discord, or run `/account create`.
        2. Create your characters with `/characters create`.
        3. Choose your main with `/characters main`.

        **Characters**
        `/characters list` – See your characters.
        `/characters create` – Create a character.
        `/characters edit` – Edit one of your characters.
        `/characters main` – Choose your main character.
        `/characters delete` – Delete one of your characters.

        **DKP**
        `/dkp balance` – See your current DKP balance.
        `/dkp history` – See your DKP history.
        `/dkp sources` – See available repeatable DKP sources.
        `/dkp request` – Request DKP from a source.
        `/dkp requests` – See your DKP requests.

        **Notifications**
        `/notifications toggle` – Toggle private review notifications on or off.

        **Shop, Bulletin Board and Achievements**
        `/shop list` – See available shop items.
        `/shop buy` – Buy an available shop item.
        `/purchases` – See your purchases.
        `/bulletin claim` – Claim an available Bulletin Board item.
        `/achievements` – See your achievements and available achievement requests.
        `/achievement request` – Request an available achievement.

        Requests and claims are reviewed by Officers. A request does not change your DKP until it is approved. Shop purchases and limits are checked automatically.

        You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.

        For questions, write in **DKP Bot Help** or contact <@333676904768864268>. The website is available at <https://wowforever.coffeecottage.dk/>.
        """;

    [SlashCommand("help", "Show the DKP Bot help and introduction", runMode: RunMode.Async)]
    public async Task HelpAsync()
    {
        await DeferAsync(ephemeral: true);
        await FollowupAsync(HelpMessage, ephemeral: true, allowedMentions: Discord.AllowedMentions.None);
    }
}
