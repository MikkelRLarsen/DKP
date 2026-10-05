using System.Text.Json;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class DkpQueries(DkpDbContext db) : IDkpQueries
{
	public async Task<DkpHistoryDto?> GetHistoryAsync(string discordId, CancellationToken ct = default)
	{
		var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct); if(user is null)return null;
		var events=await db.DkpEvents.AsNoTracking().Where(x=>x.UserId==user.Id&&(x.EventType=="DkpCredited"||x.EventType=="DkpDebited")).OrderByDescending(x=>x.OccurredAtUtc).ThenByDescending(x=>x.Id).ToArrayAsync(ct);
		if (events.Length == 0)
		{
			var legacy = await db.DkpTransactions.AsNoTracking().Include(x => x.CreatedByUser).Where(x => x.UserId == user.Id).OrderByDescending(x => x.CreatedAtUtc).Select(x => new DkpTransactionDto(x.Id,x.Amount,x.Reason,x.CreatedAtUtc,x.CreatedByUser.DiscordName)).ToArrayAsync(ct);
			return new DkpHistoryDto(new BalanceDto(legacy.Sum(x => x.Amount)), legacy);
		}
		var actorIds=events.Select(x=>x.ActorUserId).Distinct().ToArray();var names=await db.Users.AsNoTracking().Where(x=>actorIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.DiscordName,ct);
		var transactions=events.Select(x=>{using var json=JsonDocument.Parse(x.Payload);var root=json.RootElement;var amount=root.GetProperty("amount").GetInt32();var reason=root.GetProperty("reason").GetString()??x.EventType;return new DkpTransactionDto(x.Id,amount,reason,x.OccurredAtUtc,names.GetValueOrDefault(x.ActorUserId,"Unknown"));}).ToArray();
		var balance=await db.DkpBalanceProjections.AsNoTracking().Where(x=>x.UserId==user.Id).Select(x=>(int?)x.Balance).SingleOrDefaultAsync(ct)??transactions.Sum(x=>x.Amount);return new DkpHistoryDto(new BalanceDto(balance),transactions);
	}
	public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default)=>await db.Users.AsNoTracking().OrderBy(x=>x.DiscordName).Select(x=>new UserSummary(x.Id,x.DiscordId,x.DiscordName,x.AvatarUrl,x.Role,x.Characters.Where(c=>c.IsMain).Select(c=>c.FirstName+" "+c.LastName).FirstOrDefault())).ToArrayAsync(ct);
}
