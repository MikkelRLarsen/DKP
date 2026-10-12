namespace DKP.Domain;

using System.Text.Json;

public enum DkpAwardRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public sealed class DkpAwardRequest
{
    private DkpAwardRequest() { }

    public DkpAwardRequest(Guid userId, Guid? presetId, Guid? achievementId, int quantity, string? comment, DateTime now)
    {
        if ((presetId is null) == (achievementId is null)) throw new ArgumentException("Exactly one request source is required.");
        Id = Guid.NewGuid();
        UserId = userId;
        PresetId = presetId;
        AchievementId = achievementId;
        Quantity = quantity;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        Status = DkpAwardRequestStatus.Pending;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? PresetId { get; private set; }
    public Guid? AchievementId { get; private set; }
    public int Quantity { get; private set; }
    public string? Comment { get; private set; }
    public DkpAwardRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? ReviewComment { get; private set; }
    public string? DkpEventIdsJson { get; private set; }
    public Guid? DkpEventId { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public IReadOnlyList<Guid> DkpEventIds => string.IsNullOrWhiteSpace(DkpEventIdsJson) ? [] : JsonSerializer.Deserialize<Guid[]>(DkpEventIdsJson) ?? [];

    public void Cancel(DateTime now)
    {
        EnsurePending();
        Status = DkpAwardRequestStatus.Cancelled;
        CancelledAtUtc = now;
    }

    public void Approve(Guid reviewerId, IReadOnlyCollection<Guid> eventIds, string? reviewComment, DateTime now)
    {
        EnsurePending();
        Status = DkpAwardRequestStatus.Approved;
        ReviewedAtUtc = now;
        ReviewedByUserId = reviewerId;
        ReviewComment = string.IsNullOrWhiteSpace(reviewComment) ? null : reviewComment.Trim();
        DkpEventIdsJson = JsonSerializer.Serialize(eventIds);
        DkpEventId = eventIds.FirstOrDefault();
    }

    public void Reject(Guid reviewerId, string? reviewComment, DateTime now)
    {
        EnsurePending();
        Status = DkpAwardRequestStatus.Rejected;
        ReviewedAtUtc = now;
        ReviewedByUserId = reviewerId;
        ReviewComment = string.IsNullOrWhiteSpace(reviewComment) ? null : reviewComment.Trim();
    }

    private void EnsurePending()
    {
        if (Status != DkpAwardRequestStatus.Pending)
            throw new InvalidOperationException("The DKP request has already been processed.");
    }
}
