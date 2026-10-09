namespace DKP.DiscordBot;

public sealed record DiscordBotSettings(string BotToken, ulong ApplicationId, ulong GuildId, Uri ApiBaseUrl, string ApiSecret)
{
    public static DiscordBotSettings FromConfiguration(IConfiguration configuration)
    {
        var token = Required(configuration, "DISCORD_BOT_TOKEN");
        var applicationId = ParseSnowflake(configuration, "DISCORD_APPLICATION_ID");
        var guildId = ParseSnowflake(configuration, "DISCORD_GUILD_ID");
        var apiUrl = Required(configuration, "DKP_BOT_API_URL");
        var apiSecret = Required(configuration, "DKP_BOT_API_SECRET");
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseUrl)) throw new InvalidOperationException("DKP_BOT_API_URL must be a valid absolute URL.");
        return new(token, applicationId, guildId, baseUrl, apiSecret);
    }

    private static string Required(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]! : throw new InvalidOperationException($"Missing required Discord bot configuration: {key}.");
    private static ulong ParseSnowflake(IConfiguration configuration, string key) => ulong.TryParse(Required(configuration, key), out var value) && value > 0 ? value : throw new InvalidOperationException($"{key} must be a valid Discord ID.");
}
