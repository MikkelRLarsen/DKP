namespace DKP.Domain;

public sealed class UserAchievement
{
    private UserAchievement() { }
    public UserAchievement(Guid userId, Guid achievementId, Guid grantedByUserId, Guid dkpEventId, DateTime now)
    { Id = Guid.NewGuid(); UserId = userId; AchievementId = achievementId; GrantedByUserId = grantedByUserId; GrantDkpEventId = dkpEventId; GrantedAtUtc = now; }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AchievementId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTime GrantedAtUtc { get; private set; }
    public Guid GrantDkpEventId { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public Guid? RevokeDkpEventId { get; private set; }
    public void Revoke(Guid byUserId, Guid dkpEventId, DateTime now) { RevokedByUserId = byUserId; RevokeDkpEventId = dkpEventId; RevokedAtUtc = now; }
}
