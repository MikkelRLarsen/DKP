namespace DKP.Facade.Contracts;
public sealed record GuildActivityRequest(Guid? UserId = null, string? Action = null, DateTime? FromUtc = null, DateTime? ToUtc = null, int Skip = 0, int Take = 20, bool Descending = true);
public sealed record GuildActivityDto(Guid Id, Guid UserId, string UserName, string? MainCharacterName, string ActorName, string Action, int Amount, string Reason, string? ItemName, int? Quantity, DateTime CreatedAtUtc);
public sealed record GuildActivityPageDto(IReadOnlyList<GuildActivityDto> Items, int TotalCount);
