using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.DkpTransactions;

public sealed class DkpTransactionCommandService(CommandContext context, IEventLedgerRepository ledger, TimeProvider time) : IDkpTransactionCommands
{
    public async Task<DkpTransactionDto> AddAsync(CreateDkpTransactionRequest request, CancellationToken ct = default)
        => (await AddManyAsync(new([request.TargetUserId], request.Amount, request.Reason), ct))[0];
    public async Task<DkpTransactionDto> RemoveAsync(CreateDkpTransactionRequest request, CancellationToken ct = default)
        => (await RemoveManyAsync(new([request.TargetUserId], request.Amount, request.Reason), ct))[0];
    public Task<IReadOnlyList<DkpTransactionDto>> AddManyAsync(CreateDkpTransactionsRequest request, CancellationToken ct = default)
        => PostAsync(request, 1, ct);
    public Task<IReadOnlyList<DkpTransactionDto>> RemoveManyAsync(CreateDkpTransactionsRequest request, CancellationToken ct = default)
        => PostAsync(request, -1, ct);

    private Task<IReadOnlyList<DkpTransactionDto>> PostAsync(CreateDkpTransactionsRequest request, int sign, CancellationToken ct)
    {
        var targets = CommandContext.Targets(request.TargetUserIds);
        if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500) throw new ArgumentException("Reason is required (maximum 500 characters).");
        return context.ExecuteAsync<IReadOnlyList<DkpTransactionDto>>(targets, true, async actor =>
        {
            foreach (var id in targets) await context.TargetAsync(id, ct);
            var operationId = Guid.NewGuid();
            var now = time.GetUtcNow().UtcDateTime;
            var result = new List<DkpTransactionDto>();
            foreach (var id in targets)
            {
                var amount = checked(sign * request.Amount);
                var entry = await ledger.PostAsync(id, actor.Id, operationId, now, new DkpPosted(amount, reason), ct);
                result.Add(new(entry.Id, amount, reason, now, actor.DiscordName));
            }
            return result;
        }, ct);
    }
}
