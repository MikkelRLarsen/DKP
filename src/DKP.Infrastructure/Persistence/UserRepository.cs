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

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> session.Db.SaveChangesAsync(cancellationToken);

	public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
		=> await session.Db.Users.AsNoTracking().Include(x => x.Characters).OrderBy(x => x.DiscordName).ToArrayAsync(cancellationToken);
}
