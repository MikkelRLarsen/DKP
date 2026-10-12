using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpQueries(QuerySession session) : IDkpQueries
{
    public Task<DkpHistoryDto?> GetHistoryAsync(CancellationToken ct = default)
        => session.ReadAsync<DkpHistoryDto?>(false, async (db, actor) => await ReadModels.HistoryAsync(db, actor.Id, ct), ct);
    public Task<DkpHistoryDto?> GetPlayerHistoryAsync(Guid userId, CancellationToken ct = default)
        => session.ReadAsync<DkpHistoryDto?>(false, async (db, _) =>
            await db.Users.AnyAsync(x => x.Id == userId, ct) ? await ReadModels.HistoryAsync(db, userId, ct) : null, ct);
    public Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<UserSummary>>(true, async (db, _) => await ReadModels.Users(db).ToArrayAsync(ct), ct);
}
