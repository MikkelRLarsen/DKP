using System.Text.Json;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.DkpTransactions;
public sealed class DkpTransactionCommandService : IDkpTransactionCommands
{
	private readonly IUserRepository users;
	private readonly IEventLedgerRepository? ledger;
	private readonly IDkpTransactionRepository? legacyTransactions;
	private readonly TimeProvider time;
	public DkpTransactionCommandService(IUserRepository users, IEventLedgerRepository ledger, TimeProvider time) { this.users = users; this.ledger = ledger; this.time = time; }
	public DkpTransactionCommandService(IUserRepository users, IDkpTransactionRepository legacyTransactions) { this.users = users; this.legacyTransactions = legacyTransactions; time = TimeProvider.System; }
	public Task<DkpTransactionDto> AddAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken ct = default) => CreateAsync(officerDiscordId, request, 1, ct);
	public Task<DkpTransactionDto> RemoveAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken ct = default) => CreateAsync(officerDiscordId, request, -1, ct);
	private async Task<DkpTransactionDto> CreateAsync(string officerDiscordId, CreateDkpTransactionRequest request, int sign, CancellationToken ct)
	{
		var officer = await users.FindByDiscordIdAsync(officerDiscordId, ct) ?? throw new UnauthorizedAccessException("Authenticated officer does not exist.");
		if (officer.Role != UserRole.Officer) throw new UnauthorizedAccessException("Only Officers can manage DKP.");
		if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));
		var reason = request.Reason?.Trim(); if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) throw new ArgumentException("Reason is required and must be at most 500 characters.", nameof(request.Reason));
		var target = await users.FindByIdAsync(request.TargetUserId, ct) ?? throw new KeyNotFoundException("The selected player does not exist.");
		if (legacyTransactions is not null)
		{
			var now = time.GetUtcNow().UtcDateTime;
			var transaction = new DkpTransaction(target.Id, checked(request.Amount * sign), reason, officer.Id, now);
			await legacyTransactions.AddAsync(transaction, ct);
			await legacyTransactions.SaveChangesAsync(ct);
			return new DkpTransactionDto(transaction.Id, transaction.Amount, transaction.Reason, transaction.CreatedAtUtc, officer.DiscordName);
		}
		return await ledger!.WithUserLocksAsync([target.Id], async () =>
		{
			var now = time.GetUtcNow().UtcDateTime; var amount = checked(request.Amount * sign); var correlationId = Guid.NewGuid();
			var payload = JsonSerializer.Serialize(new { targetUserId = target.Id, amount, reason, actorUserId = officer.Id });
			var domainEvent = new DkpEvent("UserBalance", target.Id, await ledger!.GetNextSequenceAsync("UserBalance", target.Id, ct), amount > 0 ? "DkpCredited" : "DkpDebited", target.Id, officer.Id, now, correlationId, payload);
			await ledger.AppendAsync([domainEvent], ct);
			await ledger.ApplyBalanceAsync(target.Id, amount, domainEvent.Id, now, ct);
			await ledger.SaveChangesAsync(ct);
			return new DkpTransactionDto(domainEvent.Id, amount, reason, now, officer.DiscordName);
		}, ct);
	}
}
