using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class PlayerDetailsQueries(QuerySession session) : IPlayerDetailsQueries
{
    public Task<PlayerDetailsDto?> GetAsync(Guid userId, CancellationToken ct = default)
        => session.ReadAsync<PlayerDetailsDto?>(false, async (db, _) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
            return user == null ? null : new(user.Id, user.DiscordName, user.AvatarUrl,
                await ReadModels.Characters(db, userId).ToArrayAsync(ct), await ReadModels.HistoryAsync(db, userId, ct));
        }, ct);
}
