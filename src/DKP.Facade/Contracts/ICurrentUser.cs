namespace DKP.Facade.Contracts;
/// <summary>Identity supplied by host authentication, never by form data.</summary>
public interface ICurrentUser
{
    Task<string?> GetDiscordIdAsync(CancellationToken cancellationToken = default);
}
public enum UserRole { Member, Officer }
