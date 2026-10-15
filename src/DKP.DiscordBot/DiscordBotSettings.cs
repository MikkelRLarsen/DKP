namespace DKP.DiscordBot;

public sealed record DiscordBotSettings(
    string BotToken,
    ulong ApplicationId,
    ulong GuildId,
    Uri ApiBaseUrl,
    string ApiSecret,
    ulong? NotificationChannelId,
    ulong? MemberNotificationChannelId,
    string MemberNotificationMode,
    ulong? BulletinThreadId,
    ulong? AchievementsThreadId,
    ulong? DkpSourcesThreadId,
    ulong? ShopThreadId)
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
        var memberNotificationMode = configuration["DISCORD_MEMBER_NOTIFICATION_MODE"]?.Trim().ToLowerInvariant() ?? "text";
        if (memberNotificationMode is not ("text" or "forum"))
            throw new InvalidOperationException("DISCORD_MEMBER_NOTIFICATION_MODE must be 'text' or 'forum'.");

        var bulletinThreadId = ParseOptionalSnowflake(configuration, "DISCORD_BULLETIN_THREAD_ID");
        var achievementsThreadId = ParseOptionalSnowflake(configuration, "DISCORD_ACHIEVEMENTS_THREAD_ID");
        var dkpSourcesThreadId = ParseOptionalSnowflake(configuration, "DISCORD_DKP_SOURCES_THREAD_ID");
        var shopThreadId = ParseOptionalSnowflake(configuration, "DISCORD_SHOP_THREAD_ID");
        if (memberNotificationMode == "forum" && (bulletinThreadId is null || achievementsThreadId is null || dkpSourcesThreadId is null || shopThreadId is null))
            throw new InvalidOperationException("Forum member notifications require DISCORD_BULLETIN_THREAD_ID, DISCORD_ACHIEVEMENTS_THREAD_ID, DISCORD_DKP_SOURCES_THREAD_ID and DISCORD_SHOP_THREAD_ID.");

        return new(token, applicationId, guildId, baseUrl, apiSecret, channelId, memberChannelId, memberNotificationMode,
            bulletinThreadId, achievementsThreadId, dkpSourcesThreadId, shopThreadId);
    }

    private static string Required(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]! : throw new InvalidOperationException($"Missing required Discord bot configuration: {key}.");
    private static ulong ParseSnowflake(IConfiguration configuration, string key) => ulong.TryParse(Required(configuration, key), out var value) && value > 0 ? value : throw new InvalidOperationException($"{key} must be a valid Discord ID.");
    private static ulong ParseSnowflakeValue(string value, string key) => ulong.TryParse(value, out var result) && result > 0 ? result : throw new InvalidOperationException($"{key} must be a valid Discord ID.");
    private static ulong? ParseOptionalSnowflake(IConfiguration configuration, string key)
        => string.IsNullOrWhiteSpace(configuration[key]) ? null : ParseSnowflakeValue(configuration[key]!, key);
}
