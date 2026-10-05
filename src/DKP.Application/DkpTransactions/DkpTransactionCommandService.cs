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

    public DkpTransactionCommandService(IUserRepository users, IEventLedgerRepository ledger, TimeProvider time)
    {
        this.users = users;
        this.ledger = ledger;
        this.time = time;
    }

    public DkpTransactionCommandService(IUserRepository users, IDkpTransactionRepository legacyTransactions)
    {
        this.users = users;
        this.legacyTransactions = legacyTransactions;
        time = TimeProvider.System;
    }

    public Task<DkpTransactionDto> AddAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken ct = default) =>
        CreateAsync(officerDiscordId, request, 1, ct);

    public Task<DkpTransactionDto> RemoveAsync(string officerDiscordId, CreateDkpTransactionRequest request, CancellationToken ct = default) =>
        CreateAsync(officerDiscordId, request, -1, ct);

    public Task<IReadOnlyList<DkpTransactionDto>> AddManyAsync(string officerDiscordId, CreateDkpTransactionsRequest request, CancellationToken ct = default) =>
        CreateManyAsync(officerDiscordId, request, 1, ct);

    public Task<IReadOnlyList<DkpTransactionDto>> RemoveManyAsync(string officerDiscordId, CreateDkpTransactionsRequest request, CancellationToken ct = default) =>
        CreateManyAsync(officerDiscordId, request, -1, ct);

    private async Task<DkpTransactionDto> CreateAsync(string officerDiscordId, CreateDkpTransactionRequest request, int sign, CancellationToken ct)
    {
        var result = await CreateManyAsync(
            officerDiscordId,
            new CreateDkpTransactionsRequest([request.TargetUserId], request.Amount, request.Reason),
            sign,
            ct);
        return result[0];
    }

    private async Task<IReadOnlyList<DkpTransactionDto>> CreateManyAsync(string officerDiscordId, CreateDkpTransactionsRequest request, int sign, CancellationToken ct)
    {
        var officer = await users.FindByDiscordIdAsync(officerDiscordId, ct) ??
            throw new UnauthorizedAccessException("Authenticated officer does not exist.");
        if (officer.Role != UserRole.Officer || officer.IsBlocked)
            throw new UnauthorizedAccessException("Only active Officers can manage DKP.");
        if (request.TargetUserIds is null || request.TargetUserIds.Count == 0)
            throw new ArgumentException("At least one player must be selected.", nameof(request.TargetUserIds));
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));

        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw new ArgumentException("Reason is required and must be at most 500 characters.", nameof(request.Reason));

        var targetIds = request.TargetUserIds.Distinct().ToArray();
        var targets = new List<User>(targetIds.Length);
        foreach (var targetId in targetIds)
        {
            targets.Add(await users.FindByIdAsync(targetId, ct) ??
                throw new KeyNotFoundException("One of the selected players does not exist."));
        }

        if (legacyTransactions is not null)
        {
            var now = time.GetUtcNow().UtcDateTime;
            var transactions = targets.Select(target => new DkpTransaction(target.Id, checked(request.Amount * sign), reason, officer.Id, now)).ToArray();
            foreach (var transaction in transactions)
                await legacyTransactions.AddAsync(transaction, ct);
            await legacyTransactions.SaveChangesAsync(ct);
            return transactions.Select(transaction => new DkpTransactionDto(transaction.Id, transaction.Amount, transaction.Reason, transaction.CreatedAtUtc, officer.DiscordName)).ToArray();
        }

        var sortedTargetIds = targetIds.OrderBy(id => id).ToArray();
        return await ledger!.WithUserLocksAsync(sortedTargetIds, async () =>
        {
            var now = time.GetUtcNow().UtcDateTime;
            var amount = checked(request.Amount * sign);
            var events = new List<DkpEvent>(targets.Count);
            var result = new List<DkpTransactionDto>(targets.Count);

            foreach (var target in targets)
            {
                var eventId = Guid.NewGuid();
                var payload = JsonSerializer.Serialize(new
                {
                    targetUserId = target.Id,
                    amount,
                    reason,
                    actorUserId = officer.Id
                });
                var domainEvent = new DkpEvent(
                    "UserBalance",
                    target.Id,
                    await ledger.GetNextSequenceAsync("UserBalance", target.Id, ct),
                    amount > 0 ? "DkpCredited" : "DkpDebited",
                    target.Id,
                    officer.Id,
                    now,
                    eventId,
                    payload);
                events.Add(domainEvent);
                await ledger.ApplyBalanceAsync(target.Id, amount, eventId, now, ct);
                result.Add(new DkpTransactionDto(eventId, amount, reason, now, officer.DiscordName));
            }

            await ledger.AppendAsync(events, ct);
            await ledger.SaveChangesAsync(ct);
            return result;
        }, ct);
    }
}
