using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IGuildSettingsRepository
{
	Task<GuildSetting?> GetAsync(CancellationToken cancellationToken = default);
}
