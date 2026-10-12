using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IDkpTransactionCommands
{
	Task<DkpTransactionDto> AddAsync(CreateDkpTransactionRequest request, CancellationToken cancellationToken = default);
	Task<DkpTransactionDto> RemoveAsync(CreateDkpTransactionRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> AddManyAsync(CreateDkpTransactionsRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> RemoveManyAsync(CreateDkpTransactionsRequest request, CancellationToken cancellationToken = default);
}
