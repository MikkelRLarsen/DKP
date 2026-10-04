using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IUserRepository
{
	Task<User?> FindByDiscordIdAsync(string discordId, CancellationToken cancellationToken = default);
	Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task AddAsync(User user, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult<IReadOnlyList<User>>([]);
}
