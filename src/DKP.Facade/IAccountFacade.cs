using DKP.Facade.Contracts;

namespace DKP.Facade;

public interface IAccountFacade
{
	Task<DashboardDto?> GetDashboardAsync(string discordId, CancellationToken cancellationToken = default);
	Task<CharacterDto> CreateCharacterAsync(string discordId, CharacterInput input, CancellationToken cancellationToken = default);
	Task<CharacterDto?> UpdateCharacterAsync(string discordId, Guid characterId, CharacterInput input, CancellationToken cancellationToken = default);
	Task<bool> DeleteCharacterAsync(string discordId, Guid characterId, CancellationToken cancellationToken = default);
}
