using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class GuildSettingsRepository(CommandUnitOfWork session) : IGuildSettingsRepository
{
	public Task<GuildSetting?> GetAsync(CancellationToken ct = default) => session.Db.GuildSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
	public Task SaveChangesAsync(CancellationToken ct = default) => session.Db.SaveChangesAsync(ct);
}
