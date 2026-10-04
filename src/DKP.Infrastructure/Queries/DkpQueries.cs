using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class DkpQueries(DkpDbContext db) : IDkpQueries
{
	public async Task<DkpHistoryDto?> GetHistoryAsync(string discordId, CancellationToken cancellationToken = default)
	{
		var user = await db.Users
			.AsNoTracking()
			.SingleOrDefaultAsync(item => item.DiscordId == discordId, cancellationToken);

		if (user is null)
		{
			return null;
		}

		var transactions = await db.DkpTransactions
			.AsNoTracking()
			.Include(transaction => transaction.CreatedByUser)
			.Where(transaction => transaction.UserId == user.Id)
			.OrderByDescending(transaction => transaction.CreatedAtUtc)
			.ThenByDescending(transaction => transaction.Id)
			.Select(transaction => new DkpTransactionDto(
				transaction.Id,
				transaction.Amount,
				transaction.Reason,
				transaction.CreatedAtUtc,
				transaction.CreatedByUser.DiscordName))
			.ToArrayAsync(cancellationToken);

		return new DkpHistoryDto(
			new BalanceDto(transactions.Sum(transaction => transaction.Amount)),
			transactions);
	}

	public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
		=> await db.Users
			.AsNoTracking()
			.OrderBy(user => user.DiscordName)
			.Select(user => new UserSummary(user.Id, user.DiscordId, user.DiscordName, user.AvatarUrl, user.Role))
			.ToArrayAsync(cancellationToken);
}
