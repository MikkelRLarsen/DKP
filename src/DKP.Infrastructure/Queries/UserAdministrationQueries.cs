using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class UserAdministrationQueries(QuerySession session) : IUserAdministrationQueries
{
    public Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<UserSummary>>(true, async (db, _) => await ReadModels.Users(db).ToArrayAsync(ct), ct);
}
