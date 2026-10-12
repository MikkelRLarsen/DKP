using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class GuildActivityQueries(QuerySession session) : IGuildActivityQueries
{
    public Task<GuildActivityPageDto> GetAsync(GuildActivityRequest request, CancellationToken ct = default) => session.ReadAsync(false, async (db, _) =>
    {
        if (request.Skip < 0 || request.Take is < 1 or > 200) throw new ArgumentException("Invalid page.");
        var query = db.DkpEvents.AsNoTracking();
        if (request.UserId is Guid userId) query = query.Where(x => x.UserId == userId);
        var events = await query.ToArrayAsync(ct);
        var activities = events.GroupBy(x => x.UserId).SelectMany(group => LedgerReplayState.Replay(group).Activities)
            .Where(x => request.Action is null || x.Action == request.Action)
            .Where(x => request.FromUtc is not DateTime from || x.CreatedAtUtc >= from.ToUniversalTime())
            .Where(x => request.ToUtc is not DateTime to || x.CreatedAtUtc <= to.ToUniversalTime())
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.EventId).ToArray();
        var count = activities.Length;
        if (!request.Descending) activities = activities.Reverse().ToArray();
        var page = activities.Skip(request.Skip).Take(request.Take).ToArray();
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).ToDictionaryAsync(x => x.Id, ct);
        var result = page.Select(x => { var user = users[x.UserId]; var actor = users[x.ActorUserId]; var main = user.Characters.FirstOrDefault(c => c.IsMain); return new GuildActivityDto(x.EventId, x.UserId, user.DiscordName, main is null ? null : $"{main.FirstName} {main.LastName}", actor.DiscordName, x.Action, x.Amount, x.Reason, x.ItemName, x.Quantity, x.CreatedAtUtc); }).ToArray();
        return new GuildActivityPageDto(result, count);
    }, ct);
}
