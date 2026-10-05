using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface ILootReserveCommands
{
	Task<LootReserveSettingsDto> UpdateSettingsAsync(string officerDiscordId, UpdateLootReserveSettingsRequest request, CancellationToken cancellationToken = default);
}
