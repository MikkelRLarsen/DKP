using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IDkpTransactionCommands
{
	Task<DkpTransactionDto> AddAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken cancellationToken = default);
	Task<DkpTransactionDto> RemoveAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> AddManyAsync(string officerDiscordId, CreateDkpTransactionsRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<DkpTransactionDto>> RemoveManyAsync(string officerDiscordId, CreateDkpTransactionsRequest request, CancellationToken cancellationToken = default);
}
