using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class GuildActivityQueries(QuerySession session) : IGuildActivityQueries
{
    public Task<GuildActivityPageDto> GetAsync(GuildActivityRequest request, CancellationToken ct = default)
        => session.ReadAsync(false, async (db, _) =>
        {
            if (request.Skip < 0 || request.Take < 1 || request.Take > 200) throw new ArgumentException("Invalid page.");
            if (request.Action != null && !new[] { "manual", "preset", "purchase", "cancellation" }.Contains(request.Action))
                throw new ArgumentException("Unknown activity action.");
            var from = request.FromUtc?.ToUniversalTime();
            var to = request.ToUtc?.ToUniversalTime();
            if (from > to) throw new ArgumentException("Start date must precede end date.");
            var query = db.LedgerEntries.AsQueryable();
            if (request.UserId != null) query = query.Where(x => x.UserId == request.UserId);
            if (request.Action != null) query = query.Where(x => x.Action == request.Action);
            if (from != null) query = query.Where(x => x.CreatedAtUtc >= from);
            if (to != null) query = query.Where(x => x.CreatedAtUtc <= to);
            var count = await query.CountAsync(ct);
            var joined = from entry in query
                         join user in db.Users on entry.UserId equals user.Id
                         join actor in db.Users on entry.ActorUserId equals actor.Id
                         select new { Entry = entry, User = user, Actor = actor };
            var ordered = request.Descending
                ? joined.OrderByDescending(x => x.Entry.CreatedAtUtc).ThenByDescending(x => x.Entry.Sequence).ThenByDescending(x => x.Entry.EventId)
                : joined.OrderBy(x => x.Entry.CreatedAtUtc).ThenBy(x => x.Entry.Sequence).ThenBy(x => x.Entry.EventId);
            var rows = await ordered.Skip(request.Skip).Take(request.Take).Select(x => new GuildActivityDto(
                x.Entry.EventId, x.User.Id, x.User.DiscordName,
                x.User.Characters.Where(c => c.IsMain).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(),
                x.Actor.DiscordName, x.Entry.Action, x.Entry.Amount, x.Entry.Reason, x.Entry.ItemName, x.Entry.Quantity, x.Entry.CreatedAtUtc)).ToArrayAsync(ct);
            return new GuildActivityPageDto(rows, count);
        }, ct);
}
