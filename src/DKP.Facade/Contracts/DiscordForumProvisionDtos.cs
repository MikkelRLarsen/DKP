namespace DKP.Facade.Contracts;

public enum DiscordForumProvisionStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed record DiscordForumProvisionDto(Guid Id, string GuildId, string SectionKey, DiscordForumProvisionStatus Status,
    DateTime RequestedAtUtc, DateTime? ProcessedAtUtc, ulong? NewThreadId, ulong? NewMessageId, string? ErrorMessage);

public sealed record CompleteDiscordForumProvisionRequest(ulong ForumChannelId, ulong ThreadId, ulong MessageId);
public sealed record FailDiscordForumProvisionRequest(string Error);
