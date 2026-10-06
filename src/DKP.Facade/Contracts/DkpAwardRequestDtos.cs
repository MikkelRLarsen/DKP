namespace DKP.Facade.Contracts;

public enum DkpAwardRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public sealed record CreateDkpAwardRequestRequest(Guid PresetId, int Quantity, string? Comment);
public sealed record ReviewDkpAwardRequestRequest(string? Comment);

public sealed record DkpAwardRequestDto(
    Guid Id,
    Guid UserId,
    string DiscordName,
    string? MainCharacter,
    Guid PresetId,
    string PresetName,
    int Amount,
    int Quantity,
    string Reason,
    string? Comment,
    DkpAwardRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewedByDiscordName,
    string? ReviewComment,
    IReadOnlyList<Guid> DkpEventIds);
