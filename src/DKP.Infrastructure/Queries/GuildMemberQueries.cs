using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class GuildMemberQueries(DkpDbContext db) : IGuildMemberQueries
{
	public async Task<IReadOnlyList<GuildMemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
		=> await db.Users
			.AsNoTracking()
			.OrderBy(user => user.DiscordName)
			.Select(user => new GuildMemberDto(
				user.Id,
				user.DiscordName,
				user.AvatarUrl,
				user.DkpTransactions.Select(transaction => (int?)transaction.Amount).Sum() ?? 0,
				user.Characters
					.OrderByDescending(character => character.IsMain)
					.ThenBy(character => character.FirstName)
					.ThenBy(character => character.LastName)
					.Select(character => new CharacterDto(character.Id, character.FirstName, character.LastName, character.IsMain))
					.ToArray()))
			.ToArrayAsync(cancellationToken);
}
