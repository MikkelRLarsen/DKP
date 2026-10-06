using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class GuildMemberQueries(QuerySession session) : IGuildMemberQueries
{
    public Task<IReadOnlyList<GuildMemberDto>> GetAllAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<GuildMemberDto>>(false, async (db, _) =>
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).OrderBy(x => x.DiscordName).ThenBy(x => x.Id).ToArrayAsync(ct);
        var events = await db.DkpEvents.AsNoTracking().ToArrayAsync(ct);
        var states = events.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => DKP.Domain.LedgerReplayState.Replay(x));
        return users.Select(u => new GuildMemberDto(u.Id, u.DiscordName, u.AvatarUrl, states.GetValueOrDefault(u.Id)?.Balance ?? 0, u.Characters.OrderByDescending(c => c.IsMain).ThenBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id).Select(c => new CharacterDto(c.Id, c.FirstName, c.LastName, c.IsMain)).ToArray())).ToArray();
    }, ct);
}
