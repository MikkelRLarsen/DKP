using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.LootReserve;
public sealed class LootReserveCommandService(IUserRepository users, IGuildSettingsRepository settings) : ILootReserveCommands
{
	public async Task<LootReserveSettingsDto> UpdateSettingsAsync(string officerDiscordId, UpdateLootReserveSettingsRequest request, CancellationToken cancellationToken = default)
	{
		var officer = await users.FindByDiscordIdAsync(officerDiscordId, cancellationToken) ?? throw new UnauthorizedAccessException("The authenticated user does not exist.");
		if (officer.Role != UserRole.Officer) throw new UnauthorizedAccessException("Only Officers can manage LootReserve settings.");
		if (request.DefaultReserveLimit < 0) throw new ArgumentException("Default ReserveLimit cannot be negative.", nameof(request));
		var setting = await settings.GetAsync(cancellationToken) ?? throw new InvalidOperationException("LootReserve settings have not been initialized.");
		setting.UpdateReserveLimit(request.DefaultReserveLimit);
		await settings.SaveChangesAsync(cancellationToken);
		return new LootReserveSettingsDto(setting.DefaultReserveLimit);
	}
}
