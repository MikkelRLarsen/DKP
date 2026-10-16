namespace DKP.Facade.Contracts;

public sealed record DiscordForumSectionDto(Guid Id, string GuildId, string SectionKey, ulong? ForumChannelId,
    ulong? ThreadId, ulong? InitialMessageId, string InitialMessageContent, bool IsActive,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record UpdateDiscordForumSectionRequest(string InitialMessageContent);
public sealed record RegisterDiscordForumSectionRequest(string GuildId, string SectionKey, ulong ForumChannelId, ulong ThreadId, ulong InitialMessageId);
