using System.Reflection;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot;

public sealed class DiscordBotWorker(DiscordBotSettings settings, DkpApiClient api, ILogger<DiscordBotWorker> logger) : IHostedService, IDisposable
{
    private readonly SemaphoreSlim registrationLock = new(1, 1);
    private DiscordSocketClient? client;
    private InteractionService? interactions;
    private ServiceProvider? services;
    private CancellationTokenSource? notificationCts;
    private Task? notificationTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = false
        });
        interactions = new InteractionService(client.Rest, new InteractionServiceConfig { EnableAutocompleteHandlers = true });
        services = new ServiceCollection().AddSingleton(api).AddSingleton(settings).AddSingleton(interactions).BuildServiceProvider();
        client.Log += LogAsync;
        client.Ready += RegisterCommandsAsync;
        client.Disconnected += exception => { logger.LogWarning(exception, "Discord Gateway disconnected; the client will reconnect."); return Task.CompletedTask; };
        client.InteractionCreated += HandleInteractionAsync;
        await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
        await client.LoginAsync(TokenType.Bot, settings.BotToken);
        await client.StartAsync();
        notificationCts = new CancellationTokenSource();
        notificationTask = NotificationLoopAsync(notificationCts.Token);
        logger.LogInformation("Discord notification delivery enabled. Channel: {ChannelId}.", settings.NotificationChannelId);
        logger.LogInformation("Discord bot startup initiated for guild {GuildId}.", settings.GuildId);
    }

    private async Task NotificationLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pending = await api.GetPendingNotificationsAsync(cancellationToken, settings.NotificationChannelId is null ? "dm" : null);
                if (pending is not null)
                {
                    foreach (var notification in pending)
                    {
                        try
                        {
                            if (notification.Action == "delete")
                            {
                                if (settings.NotificationChannelId is null)
                                    throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                var channel = client?.GetChannel(settings.NotificationChannelId.Value) as IMessageChannel;
                                if (channel is null)
                                    throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                if (notification.DiscordMessageId is ulong messageId)
                                {
                                    var message = await channel.GetMessageAsync(messageId);
                                    if (message is not null) await message.DeleteAsync();
                                }
                                await api.MarkNotificationDeletedAsync(notification.Id, cancellationToken);
                            }
                            else
                            {
                                IMessage message;
                                if (ulong.TryParse(notification.RecipientDiscordUserId, out var recipientId))
                                {
                                    var recipient = await client!.Rest.GetUserAsync(recipientId);
                                    if (recipient is null)
                                        throw new InvalidOperationException("The Discord recipient could not be resolved.");
                                    var dm = await recipient.CreateDMChannelAsync();
                                    message = await dm.SendMessageAsync(FormatPrivateNotification(notification.Payload), allowedMentions: AllowedMentions.None);
                                }
                                else
                                {
                                    if (settings.NotificationChannelId is null)
                                        throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                    var channel = client?.GetChannel(settings.NotificationChannelId.Value) as IMessageChannel;
                                    if (channel is null)
                                        throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                    message = await channel.SendMessageAsync(FormatNotification(notification.Payload), allowedMentions: AllowedMentions.None);
                                }
                                await api.MarkNotificationSentAsync(notification.Id, message.Id, cancellationToken);
                            }
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            logger.LogWarning(exception, "Unable to deliver notification {NotificationId}.", notification.Id);
                            await api.MarkNotificationFailedAsync(notification.Id, exception.Message, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Discord notification polling failed.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
        }
    }

    private static string FormatNotification(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var kind = Safe(GetString(root, "Kind") ?? "DKP");
            var user = Safe(GetString(root, "UserName") ?? "Unknown user");
            var source = Safe(GetString(root, "SourceName") ?? "Unknown source");
            var amount = root.TryGetProperty("Amount", out var amountElement) ? amountElement.GetInt32() : 0;
            var quantity = root.TryGetProperty("Quantity", out var quantityElement) ? quantityElement.GetInt32() : 1;
            var comment = Safe(GetString(root, "Comment"));
            var message = $"**New {kind} DKP request**\nPlayer: {user}\nSource: {source}\nAmount: {amount}\nQuantity: {quantity}";
            return string.IsNullOrWhiteSpace(comment) ? message : $"{message}\nComment: {comment}";
        }
        catch (JsonException)
        {
            return "**New DKP request**\nA new request is awaiting officer review.";
        }
    }

    private static string FormatPrivateNotification(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var action = Safe(GetString(root, "Action") ?? "updated");
            var kind = Safe(GetString(root, "Kind") ?? "DKP");
            var source = Safe(GetString(root, "SourceName") ?? GetString(root, "ItemName") ?? "item");
            var amount = root.TryGetProperty("Amount", out var amountElement) ? amountElement.GetInt32() : 0;
            var quantity = root.TryGetProperty("Quantity", out var quantityElement) ? quantityElement.GetInt32() : 1;
            var comment = Safe(GetString(root, "ReviewComment"));
            var reviewer = Safe(GetString(root, "ReviewerName") ?? GetString(root, "OfficerName"));
            var message = $"Your {kind.ToLowerInvariant()} request was **{action}**.\nSource: {source}\nAmount: {amount} DKP\nQuantity: {quantity}";
            if (!string.IsNullOrWhiteSpace(reviewer)) message += $"\nProcessed by: {reviewer}";
            if (!string.IsNullOrWhiteSpace(comment)) message += $"\nComment: {comment}";
            return message;
        }
        catch (JsonException)
        {
            return "Your DKP request was processed. Please check the DKP website for details.";
        }
    }

    private static string? GetString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Safe(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("@", "@\u200b", StringComparison.Ordinal);

    private async Task RegisterCommandsAsync()
    {
        if (client is null || interactions is null) return;
        await registrationLock.WaitAsync();
        try
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await interactions.RegisterCommandsToGuildAsync(settings.GuildId, true);
                    logger.LogInformation("Registered Discord commands for guild {GuildId}.", settings.GuildId);
                    return;
                }
                catch (Exception exception)
                {
                    if (attempt == 3)
                    {
                        logger.LogError(exception, "Unable to register Discord commands for guild {GuildId} after 3 attempts.", settings.GuildId);
                        return;
                    }

                    logger.LogWarning(exception, "Unable to register Discord commands for guild {GuildId}; retry {Attempt}/3.", settings.GuildId, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
        }
        finally { registrationLock.Release(); }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        if (interactions is null || services is null || client is null) return;
        var context = new SocketInteractionContext(client, interaction);
        try { await interactions.ExecuteCommandAsync(context, services); }
        catch (Exception exception) { logger.LogError(exception, "Discord interaction failed."); }
    }

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is null) logger.LogInformation("Discord: {Message}", message.Message);
        else logger.LogWarning(message.Exception, "Discord: {Message}", message.Message);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (notificationCts is not null)
        {
            notificationCts.Cancel();
            if (notificationTask is not null)
            {
                try { await notificationTask; }
                catch (OperationCanceledException) { }
            }
            notificationCts.Dispose();
            notificationCts = null;
            notificationTask = null;
        }
        if (client is not null)
        {
            await client.StopAsync();
            await client.LogoutAsync();
        }
    }

    public void Dispose()
    {
        services?.Dispose();
        client?.Dispose();
        registrationLock.Dispose();
    }
}
