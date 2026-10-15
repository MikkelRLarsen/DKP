using System.Text.Json;

namespace DKP.Domain;

public enum BulletinClaimStatus { Pending, Approved, Rejected, Cancelled, Reverted }

public sealed class BulletinClaim
{
    private BulletinClaim() { }
    public BulletinClaim(Guid bulletinItemId, Guid userId, int quantity, string? comment, DateTime now)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        Id = Guid.NewGuid(); BulletinItemId = bulletinItemId; UserId = userId; Quantity = quantity;
        Comment = Normalize(comment); Status = BulletinClaimStatus.Pending; CreatedAtUtc = now;
    }
    public Guid Id { get; private set; }
    public Guid BulletinItemId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public string? Comment { get; private set; }
    public BulletinClaimStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? ReviewComment { get; private set; }
    public string? DkpEventIdsJson { get; private set; }
    public IReadOnlyList<Guid> DkpEventIds => string.IsNullOrWhiteSpace(DkpEventIdsJson) ? [] : JsonSerializer.Deserialize<Guid[]>(DkpEventIdsJson) ?? [];
    public void Cancel(DateTime now) { EnsurePending(); Status = BulletinClaimStatus.Cancelled; ReviewedAtUtc = now; }
    public void Reject(Guid reviewerId, string? comment, DateTime now) { EnsurePending(); Status = BulletinClaimStatus.Rejected; ReviewedByUserId = reviewerId; ReviewedAtUtc = now; ReviewComment = Normalize(comment); }
    public void Approve(Guid reviewerId, IReadOnlyCollection<Guid> eventIds, string? comment, DateTime now) { EnsurePending(); if (eventIds.Count == 0) throw new ArgumentException("At least one DKP event is required."); Status = BulletinClaimStatus.Approved; ReviewedByUserId = reviewerId; ReviewedAtUtc = now; ReviewComment = Normalize(comment); DkpEventIdsJson = JsonSerializer.Serialize(eventIds); }
    public void Revert(Guid reviewerId, Guid reversalEventId, string? comment, DateTime now)
    {
        if (Status != BulletinClaimStatus.Approved) throw new InvalidOperationException("Only approved claims can be reverted.");
        if (reversalEventId == Guid.Empty) throw new ArgumentException("A reversal event is required.");
        Status = BulletinClaimStatus.Reverted; ReviewedByUserId = reviewerId; ReviewedAtUtc = now; ReviewComment = Normalize(comment); DkpEventIdsJson = JsonSerializer.Serialize(DkpEventIds.Append(reversalEventId));
    }
    private void EnsurePending() { if (Status != BulletinClaimStatus.Pending) throw new InvalidOperationException("The bulletin claim has already been processed."); }
    private static string? Normalize(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); if (result.Length > 500) throw new ArgumentException("Comment must be at most 500 characters."); return result; }
}
