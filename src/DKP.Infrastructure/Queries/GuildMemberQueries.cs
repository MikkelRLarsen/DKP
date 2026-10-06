using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class GuildMemberQueries(QuerySession session) : IGuildMemberQueries
{
    public Task<IReadOnlyList<GuildMemberDto>> GetAllAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<GuildMemberDto>>(false, async (db, _) =>
            await db.Users.OrderBy(u => u.DiscordName).ThenBy(u => u.Id).Select(u => new GuildMemberDto(
                u.Id, u.DiscordName, u.AvatarUrl,
                db.DkpBalanceProjections.Where(b => b.UserId == u.Id).Select(b => b.Balance).FirstOrDefault(),
                u.Characters.OrderByDescending(c => c.IsMain).ThenBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
                    .Select(c => new CharacterDto(c.Id, c.FirstName, c.LastName, c.IsMain)).ToList())).ToArrayAsync(ct), ct);
}
