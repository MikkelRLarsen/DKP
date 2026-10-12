namespace DKP.DiscordBot;

public sealed record DiscordBotSettings(string BotToken, ulong ApplicationId, ulong GuildId, Uri ApiBaseUrl, string ApiSecret, ulong? NotificationChannelId, ulong? MemberNotificationChannelId)
{
    public static DiscordBotSettings FromConfiguration(IConfiguration configuration)
    {
        var token = Required(configuration, "DISCORD_BOT_TOKEN");
        var applicationId = ParseSnowflake(configuration, "DISCORD_APPLICATION_ID");
        var guildId = ParseSnowflake(configuration, "DISCORD_GUILD_ID");
        var apiUrl = Required(configuration, "DKP_BOT_API_URL");
        var apiSecret = Required(configuration, "DKP_BOT_API_SECRET");
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseUrl)) throw new InvalidOperationException("DKP_BOT_API_URL must be a valid absolute URL.");
        var notificationChannel = configuration["DISCORD_OFFICER_CHANNEL_ID"];
        ulong? channelId = string.IsNullOrWhiteSpace(notificationChannel) ? null : ParseSnowflakeValue(notificationChannel, "DISCORD_OFFICER_CHANNEL_ID");
        var memberNotificationChannel = configuration["DISCORD_MEDLEM_CHANNEL_ID"];
        ulong? memberChannelId = string.IsNullOrWhiteSpace(memberNotificationChannel) ? null : ParseSnowflakeValue(memberNotificationChannel, "DISCORD_MEDLEM_CHANNEL_ID");
        return new(token, applicationId, guildId, baseUrl, apiSecret, channelId, memberChannelId);
    }

    private static string Required(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]! : throw new InvalidOperationException($"Missing required Discord bot configuration: {key}.");
    private static ulong ParseSnowflake(IConfiguration configuration, string key) => ulong.TryParse(Required(configuration, key), out var value) && value > 0 ? value : throw new InvalidOperationException($"{key} must be a valid Discord ID.");
    private static ulong ParseSnowflakeValue(string value, string key) => ulong.TryParse(value, out var result) && result > 0 ? result : throw new InvalidOperationException($"{key} must be a valid Discord ID.");
}
