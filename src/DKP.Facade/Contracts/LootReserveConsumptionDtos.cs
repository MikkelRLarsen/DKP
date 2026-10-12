namespace DKP.Facade.Contracts;

public sealed record LootReserveConsumptionPreviewDto(Guid UserId, string DiscordName, int SoftReserveQuantity, int? RollBonusValue, bool IsReady);
public sealed record LootReserveConsumptionBatchDto(Guid BatchId, DateTime CreatedAtUtc, string CreatedByDiscordName, int PlayerCount, int SoftReserveQuantity, int RollBonusCount, bool CanRevert);
public sealed record LootReserveConsumptionResultDto(LootReserveConsumptionBatchDto Batch, IReadOnlyList<LootReserveConsumptionPreviewDto> Players);
