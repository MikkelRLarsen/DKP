namespace DKP.Facade.Contracts;

public sealed record CreateDkpTransactionRequest(Guid TargetUserId, int Amount, string Reason);
public sealed record CreateDkpTransactionsRequest(IReadOnlyCollection<Guid> TargetUserIds, int Amount, string Reason);
public sealed record DkpTransactionDto(Guid Id, int Amount, string Reason, DateTime CreatedAtUtc, string CreatedByDiscordName);
public sealed record BalanceDto(int Amount);
public sealed record DkpHistoryDto(BalanceDto Balance, IReadOnlyList<DkpTransactionDto> Transactions);
