using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IDkpTransactionRepository
{
	Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
	Task AddAsync(DkpTransaction transaction, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
