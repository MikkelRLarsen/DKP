using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpTransactionRepository(DkpDbContext db) : IDkpTransactionRepository
{
	public async Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var amounts = await db.DkpTransactions
			.AsNoTracking()
			.Where(transaction => transaction.UserId == userId)
			.Select(transaction => transaction.Amount)
			.ToArrayAsync(cancellationToken);

		return amounts.Sum();
	}

	public Task AddAsync(DkpTransaction transaction, CancellationToken cancellationToken = default)
	{
		db.DkpTransactions.Add(transaction);
		return Task.CompletedTask;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> db.SaveChangesAsync(cancellationToken);
}
