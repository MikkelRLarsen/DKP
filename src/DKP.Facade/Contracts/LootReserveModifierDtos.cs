namespace DKP.Facade.Contracts;

public enum LootReserveModifierKind { SoftReserve, RollBonus }
public sealed record CreateLootReserveModifierRequest(LootReserveModifierKind Kind, int Amount, int ExportCount, string Reason);
public sealed record LootReserveModifierDto(Guid ModifierId, Guid UserId, string DiscordName, LootReserveModifierKind Kind, int Amount, int RemainingExports, string Reason, DateTime CreatedAtUtc);
