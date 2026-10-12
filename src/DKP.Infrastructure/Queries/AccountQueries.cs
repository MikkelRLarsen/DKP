using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class AccountQueries(QuerySession session) : IAccountQueries
{
    public Task<DashboardDto?> GetDashboardAsync(CancellationToken ct = default)
        => session.ReadAsync<DashboardDto?>(false, async (db, user) => new(
            user.Id, user.DiscordId, user.DiscordName, user.AvatarUrl, ReadModels.Role(user.Role),
            (await ReadModels.StateAsync(db, user.Id, ct)).Balance,
            await ReadModels.Characters(db, user.Id).ToArrayAsync(ct)), ct);
}
