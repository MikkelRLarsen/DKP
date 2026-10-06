using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.LootReserve;

public sealed class LootReserveCommandService(CommandContext context, IGuildSettingsRepository settings) : ILootReserveCommands
{
    public Task<LootReserveSettingsDto> UpdateSettingsAsync(UpdateLootReserveSettingsRequest request, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            if (request.DefaultReserveLimit < 0) throw new ArgumentException("Default ReserveLimit cannot be negative.");
            var setting = await settings.GetAsync(ct) ?? throw new InvalidOperationException("Guild settings not initialized.");
            setting.UpdateReserveLimit(request.DefaultReserveLimit);
            return new LootReserveSettingsDto(setting.DefaultReserveLimit);
        }, ct);
}
