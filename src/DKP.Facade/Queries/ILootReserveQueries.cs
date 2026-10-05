using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface ILootReserveQueries
{
	Task<IReadOnlyList<LootReserveMemberDto>> GetMembersAsync(string authenticatedDiscordId, CancellationToken cancellationToken = default);
	Task<LootReserveSettingsDto> GetSettingsAsync(string authenticatedDiscordId, CancellationToken cancellationToken = default);
}
