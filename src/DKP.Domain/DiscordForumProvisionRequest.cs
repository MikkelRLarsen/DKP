namespace DKP.Domain;

public sealed class DiscordForumProvisionRequest
{
    private DiscordForumProvisionRequest() { }

    public DiscordForumProvisionRequest(string guildId, string sectionKey, DateTime requestedAtUtc)
    {
        Id = Guid.NewGuid();
        GuildId = Required(guildId, 32, nameof(guildId));
        SectionKey = Required(sectionKey, 64, nameof(sectionKey));
        Status = DiscordForumProvisionRequestStatus.Pending;
        RequestedAtUtc = requestedAtUtc;
    }

    public Guid Id { get; private set; }
    public string GuildId { get; private set; } = string.Empty;
    public string SectionKey { get; private set; } = string.Empty;
    public DiscordForumProvisionRequestStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public ulong? NewThreadId { get; private set; }
    public ulong? NewMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkProcessing() => Status = DiscordForumProvisionRequestStatus.Processing;

    public void Complete(ulong threadId, ulong messageId, DateTime now)
    {
        Status = DiscordForumProvisionRequestStatus.Completed;
        ProcessedAtUtc = now;
        NewThreadId = threadId;
        NewMessageId = messageId;
        ErrorMessage = null;
    }

    public void Fail(string error, DateTime now)
    {
        Status = DiscordForumProvisionRequestStatus.Failed;
        ProcessedAtUtc = now;
        ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Unknown provisioning error." : error[..Math.Min(1000, error.Length)];
    }

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.");
        var result = value.Trim();
        if (result.Length > max) throw new ArgumentException($"{name} must be at most {max} characters.");
        return result;
    }
}
