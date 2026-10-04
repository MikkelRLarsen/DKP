using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class PlayerDetailsQueries(DkpDbContext db) : IPlayerDetailsQueries
{
	public async Task<PlayerDetailsDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var user = await db.Users
			.AsNoTracking()
			.Include(item => item.Characters)
			.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

		if (user is null)
		{
			return null;
		}

		var transactions = await db.DkpTransactions
			.AsNoTracking()
			.Include(transaction => transaction.CreatedByUser)
			.Where(transaction => transaction.UserId == userId)
			.OrderByDescending(transaction => transaction.CreatedAtUtc)
			.ThenByDescending(transaction => transaction.Id)
			.Select(transaction => new DkpTransactionDto(
				transaction.Id,
				transaction.Amount,
				transaction.Reason,
				transaction.CreatedAtUtc,
				transaction.CreatedByUser.DiscordName))
			.ToArrayAsync(cancellationToken);

		var characters = user.Characters
			.OrderByDescending(character => character.IsMain)
			.ThenBy(character => character.FirstName)
			.ThenBy(character => character.LastName)
			.Select(character => new CharacterDto(character.Id, character.FirstName, character.LastName, character.IsMain))
			.ToArray();

		return new PlayerDetailsDto(
			user.Id,
			user.DiscordName,
			user.AvatarUrl,
			characters,
			new DkpHistoryDto(new BalanceDto(transactions.Sum(transaction => transaction.Amount)), transactions));
	}
}
