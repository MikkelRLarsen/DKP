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
		DkpTransactions = new List<DkpTransaction>();
		CreatedDkpTransactions = new List<DkpTransaction>();
		SoftReservePurchases = new List<SoftReservePurchase>();
	}

	public Guid Id { get; private set; }
	public string DiscordId { get; private set; } = string.Empty;
	public string DiscordName { get; private set; } = string.Empty;
	public string? AvatarUrl { get; private set; }
	public UserRole Role { get; private set; }
	public DateTime CreatedAtUtc { get; private set; }
	public ICollection<Character> Characters { get; private set; } = new List<Character>();
	public ICollection<DkpTransaction> DkpTransactions { get; private set; } = new List<DkpTransaction>();
	public ICollection<DkpTransaction> CreatedDkpTransactions { get; private set; } = new List<DkpTransaction>();
	public ICollection<SoftReservePurchase> SoftReservePurchases { get; private set; } = new List<SoftReservePurchase>();

	public void UpdateProfile(string discordName, string? avatarUrl, UserRole role)
	{
		DiscordName = discordName;
		AvatarUrl = avatarUrl;
		Role = role;
	}

	public void SetRole(UserRole role) => Role = role;
}
