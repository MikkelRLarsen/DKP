using System.Text;
using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot;

public sealed class DkpModule(DkpApiClient api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("dkp", "Shows your current DKP balance", runMode: RunMode.Async)]
    public async Task BalanceAsync()
    {
        await DeferAsync(ephemeral: true);
        var history = await api.GetDkpAsync(Context.User.Id.ToString(), CancellationToken.None);
        if (history is null)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = "Your DKP account could not be loaded.");
            return;
        }

        await ModifyOriginalResponseAsync(properties => properties.Content = $"Your current DKP balance is **{history.Balance.Amount}**.");
    }

    [SlashCommand("dkp-history", "Shows your recent DKP history", runMode: RunMode.Async)]
    public async Task HistoryAsync()
    {
        await DeferAsync(ephemeral: true);
        var history = await api.GetDkpHistoryAsync(Context.User.Id.ToString(), 10, CancellationToken.None);
        if (history is null)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = "Your DKP history could not be loaded.");
            return;
        }

        if (history.Transactions.Count == 0)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = $"Your current DKP balance is **{history.Balance.Amount}**.\nNo DKP history found.");
            return;
        }

        var output = new StringBuilder($"Current balance: **{history.Balance.Amount}**\n");
        foreach (var transaction in history.Transactions)
        {
            var sign = transaction.Amount >= 0 ? "+" : string.Empty;
            output.AppendLine($"`{transaction.CreatedAtUtc:yyyy-MM-dd}` **{sign}{transaction.Amount}** — {transaction.Reason} ({transaction.CreatedByDiscordName})");
        }

        await ModifyOriginalResponseAsync(properties => properties.Content = output.ToString());
    }
}
