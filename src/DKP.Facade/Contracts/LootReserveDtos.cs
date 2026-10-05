namespace DKP.Facade.Contracts;
public sealed record LootReserveMemberDto(Guid UserId, string DiscordName, IReadOnlyList<CharacterDto> Characters, int ReserveLimit, int RollBonus, bool IsReady);
public sealed record LootReserveSettingsDto(int DefaultReserveLimit);
public sealed record UpdateLootReserveSettingsRequest(int DefaultReserveLimit);
