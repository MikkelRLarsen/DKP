namespace DKP.Domain;

public sealed class User
{
	private User()
	{
	}

	public User(string discordId, string discordName, string? avatarUrl, UserRole role, DateTime createdAtUtc)
	{
		Id = Guid.NewGuid();
		DiscordId = discordId;
		DiscordName = discordName;
		AvatarUrl = avatarUrl;
		Role = role;
		CreatedAtUtc = createdAtUtc;
		Characters = new List<Character>();
	}

	public Guid Id { get; private set; }
	public string DiscordId { get; private set; } = string.Empty;
	public string DiscordName { get; private set; } = string.Empty;
	public string? AvatarUrl { get; private set; }
	public UserRole Role { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public bool IsBlocked { get; private set; }
	public DateTime? BlockedAtUtc { get; private set; }
	public Guid? BlockedByUserId { get; private set; }
	public string? BlockReason { get; private set; }
	public ICollection<Character> Characters { get; private set; } = new List<Character>();

	public void UpdateProfile(string discordName, string? avatarUrl, UserRole role)
	{
		DiscordName = discordName;
		AvatarUrl = avatarUrl;
		Role = role;
	}

	public void SetRole(UserRole role) => Role = role;
	public void Block(Guid blockedByUserId, string? reason, DateTime now) { IsBlocked = true; BlockedByUserId = blockedByUserId; BlockedAtUtc = now; BlockReason = reason; }
	public void Unblock() { IsBlocked = false; BlockedByUserId = null; BlockedAtUtc = null; BlockReason = null; }
}
