using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class UserRepository(CommandUnitOfWork session) : IUserRepository
{
	public Task<User?> FindByDiscordIdAsync(string discordId, CancellationToken cancellationToken = default)
		=> session.Db.Users.SingleOrDefaultAsync(user => user.DiscordId == discordId, cancellationToken);

	public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
		=> session.Db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

	public Task AddAsync(User user, CancellationToken cancellationToken = default)
	{
		session.Db.Users.Add(user);
		return Task.CompletedTask;
	}
}
