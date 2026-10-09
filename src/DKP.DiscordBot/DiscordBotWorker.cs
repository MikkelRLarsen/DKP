using System.Reflection;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = false
        });
        interactions = new InteractionService(client.Rest);
        services = new ServiceCollection().AddSingleton(api).AddSingleton(settings).AddSingleton(interactions).BuildServiceProvider();
        client.Log += LogAsync;
        client.Ready += RegisterCommandsAsync;
        client.Disconnected += exception => { logger.LogWarning(exception, "Discord Gateway disconnected; the client will reconnect."); return Task.CompletedTask; };
        client.InteractionCreated += HandleInteractionAsync;
        await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
        await client.LoginAsync(TokenType.Bot, settings.BotToken);
        await client.StartAsync();
        logger.LogInformation("Discord bot startup initiated for guild {GuildId}.", settings.GuildId);
    }

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
