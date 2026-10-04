using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IDkpTransactionRepository
{
	Task AddAsync(DkpTransaction transaction, CancellationToken cancellationToken = default);
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
