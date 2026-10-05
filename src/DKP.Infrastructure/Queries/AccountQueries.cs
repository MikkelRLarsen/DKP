using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class AccountQueries(DkpDbContext db) : IAccountQueries
{
	public async Task<DashboardDto?> GetDashboardAsync(string discordId, CancellationToken ct = default)
	{
		var user=await db.Users.AsNoTracking().Include(x=>x.Characters).SingleOrDefaultAsync(x=>x.DiscordId==discordId,ct);if(user is null)return null;
		var projection=await db.DkpBalanceProjections.AsNoTracking().Where(x=>x.UserId==user.Id).Select(x=>(int?)x.Balance).SingleOrDefaultAsync(ct);var balance=projection??await db.DkpTransactions.Where(x=>x.UserId==user.Id).Select(x=>(int?)x.Amount).SumAsync(ct)??0;
		return new DashboardDto(user.Id,user.DiscordId,user.DiscordName,user.AvatarUrl,user.Role,balance,user.Characters.OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).Select(x=>new CharacterDto(x.Id,x.FirstName,x.LastName,x.IsMain)).ToArray());
	}
}
