using DKP.Application.Persistence;
using DKP.Domain;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpTransactionRepository(DkpDbContext db) : IDkpTransactionRepository
{
	public Task AddAsync(DkpTransaction transaction, CancellationToken cancellationToken = default)
	{
		db.DkpTransactions.Add(transaction);
		return Task.CompletedTask;
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
		=> db.SaveChangesAsync(cancellationToken);
}
