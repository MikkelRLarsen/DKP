namespace DKP.Domain;

public sealed class DiscordForumSection
{
    private DiscordForumSection() { }

    public DiscordForumSection(string guildId, string sectionKey, string defaultContent, DateTime now)
    {
        Id = Guid.NewGuid();
        GuildId = Required(guildId, 32, nameof(guildId));
        SectionKey = Required(sectionKey, 64, nameof(sectionKey));
        InitialMessageContent = Required(defaultContent, 4000, nameof(defaultContent));
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public string GuildId { get; private set; } = string.Empty;
    public string SectionKey { get; private set; } = string.Empty;
    public ulong? ForumChannelId { get; private set; }
    public ulong? ThreadId { get; private set; }
    public ulong? InitialMessageId { get; private set; }
    public string InitialMessageContent { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void SetContent(string content, DateTime now)
    {
        InitialMessageContent = Required(content, 4000, nameof(content));
        UpdatedAtUtc = now;
    }

    public void Register(ulong forumChannelId, ulong threadId, ulong initialMessageId, DateTime now)
    {
        ForumChannelId = forumChannelId;
        ThreadId = threadId;
        InitialMessageId = initialMessageId;
        UpdatedAtUtc = now;
    }

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.");
        var result = value.Trim();
        if (result.Length > max) throw new ArgumentException($"{name} must be at most {max} characters.");
        return result;
    }
}
