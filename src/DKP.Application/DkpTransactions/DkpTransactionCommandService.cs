using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpTransactions;

public sealed class DkpTransactionCommandService(
	IUserRepository users,
	IDkpTransactionRepository transactions) : IDkpTransactionCommands
{
	public Task<DkpTransactionDto> AddAsync(
		string officerDiscordId,
		CreateDkpTransactionRequest request,
		CancellationToken cancellationToken = default)
		=> CreateAsync(officerDiscordId, request, 1, cancellationToken);

	public Task<DkpTransactionDto> RemoveAsync(
		string officerDiscordId,
		CreateDkpTransactionRequest request,
		CancellationToken cancellationToken = default)
		=> CreateAsync(officerDiscordId, request, -1, cancellationToken);

	private async Task<DkpTransactionDto> CreateAsync(
		string officerDiscordId,
		CreateDkpTransactionRequest request,
		int sign,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(officerDiscordId))
		{
			throw new UnauthorizedAccessException("An authenticated officer is required.");
		}

		var officer = await users.FindByDiscordIdAsync(officerDiscordId, cancellationToken)
			?? throw new UnauthorizedAccessException("The authenticated officer does not exist.");

		if (officer.Role != UserRole.Officer)
		{
			throw new UnauthorizedAccessException("Only Officers can manage DKP.");
		}

		if (request.Amount <= 0)
		{
			throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));
		}

		var reason = request.Reason?.Trim();
		if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
		{
			throw new ArgumentException("Reason is required and must be at most 500 characters.", nameof(request.Reason));
		}

		var target = await users.FindByIdAsync(request.TargetUserId, cancellationToken)
			?? throw new KeyNotFoundException("The selected player does not exist.");

		var transaction = new DkpTransaction(
			target.Id,
			checked(request.Amount * sign),
			reason,
			officer.Id,
			DateTime.UtcNow);

		await transactions.AddAsync(transaction, cancellationToken);
		await transactions.SaveChangesAsync(cancellationToken);

		return new DkpTransactionDto(
			transaction.Id,
			transaction.Amount,
			transaction.Reason,
			transaction.CreatedAtUtc,
			officer.DiscordName);
	}
}
