using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface ILootReserveQueries
{
	Task<IReadOnlyList<LootReserveMemberDto>> GetMembersAsync(CancellationToken cancellationToken = default);
	Task<LootReserveSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
}
