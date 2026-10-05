using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Persistence;
public sealed class GuildSettingsRepository(DkpDbContext db) : IGuildSettingsRepository
{
	public Task<GuildSetting?> GetAsync(CancellationToken ct = default) => db.GuildSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
	public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
