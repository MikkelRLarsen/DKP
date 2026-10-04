using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class AccountQueries(DkpDbContext db) : IAccountQueries
{
	public async Task<DashboardDto?> GetDashboardAsync(string discordId, CancellationToken cancellationToken = default)
	{
		var user = await db.Users
			.AsNoTracking()
			.Include(item => item.Characters)
			.Where(item => item.DiscordId == discordId)
			.Select(item => new
			{
				User = item,
				DkpBalance = item.DkpTransactions.Sum(transaction => (int?)transaction.Amount) ?? 0
			})
			.SingleOrDefaultAsync(cancellationToken);

		return user is null
			? null
			: new DashboardDto(
				user.User.Id,
				user.User.DiscordId,
				user.User.DiscordName,
				user.User.AvatarUrl,
				user.User.Role,
				user.DkpBalance,
				user.User.Characters
					.OrderBy(character => character.FirstName)
					.ThenBy(character => character.LastName)
					.Select(character => new CharacterDto(character.Id, character.FirstName, character.LastName))
					.ToArray());
	}
}
