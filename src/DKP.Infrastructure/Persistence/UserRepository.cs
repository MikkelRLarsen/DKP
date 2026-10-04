using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class UserRepository(DkpDbContext db) : IUserRepository
{
	public Task<User?> FindByDiscordIdAsync(string discordId, CancellationToken cancellationToken = default)
		=> db.Users.SingleOrDefaultAsync(user => user.DiscordId == discordId, cancellationToken);

	public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
		=> db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

	public Task AddAsync(User user, CancellationToken cancellationToken = default)
	{
		db.Users.Add(user);
		return Task.CompletedTask;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> db.SaveChangesAsync(cancellationToken);
}
