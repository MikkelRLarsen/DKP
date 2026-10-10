namespace DKP.Domain;

public sealed class DiscordNotificationOutbox
{
    private DiscordNotificationOutbox() { }

    public DiscordNotificationOutbox(string notificationType, string payload, DateTime createdAtUtc, Guid? requestId = null, string? recipientDiscordUserId = null)
    {
        Id = Guid.NewGuid();
        NotificationType = notificationType;
        Payload = payload;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
        RequestId = requestId;
        RecipientDiscordUserId = recipientDiscordUserId;
    }

    public Guid Id { get; private set; }
    public Guid? RequestId { get; private set; }
    public string? RecipientDiscordUserId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? ClaimedUntilUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public ulong? DiscordMessageId { get; private set; }
    public DateTime? DeleteRequestedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public void Claim(DateTime now, DateTime until)
    {
        Attempts++;
        ClaimedUntilUtc = until;
        LastError = null;
    }

    public void MarkSent(DateTime now, ulong messageId)
    {
        SentAtUtc = now;
        DiscordMessageId = messageId;
        ClaimedUntilUtc = null;
    }

    public void RequestDeletion(DateTime now)
    {
        DeleteRequestedAtUtc ??= now;
        ClaimedUntilUtc = null;
        NextAttemptAtUtc = now;
    }

    public void MarkDeleted(DateTime now)
    {
        DeletedAtUtc = now;
        ClaimedUntilUtc = null;
    }

    public void MarkFailed(DateTime now, string error)
    {
        ClaimedUntilUtc = null;
        NextAttemptAtUtc = now.AddSeconds(Math.Min(300, Math.Max(5, Attempts * 10)));
        LastError = error.Length > 500 ? error[..500] : error;
    }
}
