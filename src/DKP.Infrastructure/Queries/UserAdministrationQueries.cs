using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class UserAdministrationQueries(DkpDbContext db) : IUserAdministrationQueries
{
	public async Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken = default)
		=> await db.Users
			.AsNoTracking()
			.OrderBy(user => user.DiscordName)
			.Select(user => new UserSummary(
				user.Id,
				user.DiscordId,
				user.DiscordName,
				user.AvatarUrl,
				user.Role,
				user.Characters
					.Where(character => character.IsMain)
					.Select(character => character.FirstName + " " + character.LastName)
					.FirstOrDefault(),
				user.IsBlocked,
				user.BlockedAtUtc,
				user.BlockReason))
			.ToArrayAsync(cancellationToken);
}
