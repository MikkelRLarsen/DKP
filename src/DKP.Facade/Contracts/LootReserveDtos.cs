namespace DKP.Facade.Contracts;
public sealed record LootReserveMemberDto(Guid UserId, string DiscordName, IReadOnlyList<CharacterDto> Characters, int ExtraReserve, int RollBonus, bool IsReady);
