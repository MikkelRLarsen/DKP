using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface ILootReserveCommands
{
	Task<LootReserveSettingsDto> UpdateSettingsAsync(UpdateLootReserveSettingsRequest request, CancellationToken cancellationToken = default);
}
