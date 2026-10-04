namespace DKP.Domain;

public sealed class Character
{
	private Character()
	{
	}

	public Character(Guid userId, string firstName, string lastName)
	{
		Id = Guid.NewGuid();
		UserId = userId;
		FirstName = firstName;
		LastName = lastName;
	}

	public Guid Id { get; private set; }
	public Guid UserId { get; private set; }
	public string FirstName { get; private set; } = string.Empty;
	public string LastName { get; private set; } = string.Empty;
	public User User { get; private set; } = null!;

	public void Update(string firstName, string lastName)
	{
		FirstName = firstName;
		LastName = lastName;
	}
}
