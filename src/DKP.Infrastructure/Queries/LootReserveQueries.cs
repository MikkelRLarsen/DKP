using DKP.Application.Authentication;
using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class LootReserveQueries(DkpDbContext db) : ILootReserveQueries
{
	private async Task EnsureOfficerAsync(string discordId, CancellationToken ct)
	{
		var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct) ?? throw new UnauthorizedAccessException("The authenticated user does not exist.");
		if (user.Role != UserRole.Officer) throw new UnauthorizedAccessException("Only Officers can view LootReserve exports.");
	}
	public async Task<IReadOnlyList<LootReserveMemberDto>> GetMembersAsync(string discordId, CancellationToken ct = default)
	{
		await EnsureOfficerAsync(discordId, ct);
		var setting = await db.GuildSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
		var defaultLimit = setting?.DefaultReserveLimit ?? 0;
		return await db.Users.AsNoTracking().OrderBy(x => x.DiscordName).Select(user => new LootReserveMemberDto(user.Id,user.DiscordName,user.Characters.OrderByDescending(c => c.IsMain).ThenBy(c => c.LastName).ThenBy(c => c.FirstName).Select(c => new CharacterDto(c.Id,c.FirstName,c.LastName,c.IsMain)).ToArray(),defaultLimit + (user.ShopPurchases.Where(p => p.CancelledAtUtc == null && p.ShopItem.Key == "soft-reserve").Select(p => (int?)p.Quantity).Sum() ?? 0),user.RollBonus,user.Characters.Any())).ToArrayAsync(ct);
	}
	public async Task<LootReserveSettingsDto> GetSettingsAsync(string discordId, CancellationToken ct = default)
	{
		await EnsureOfficerAsync(discordId, ct);
		var setting = await db.GuildSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
		return new LootReserveSettingsDto(setting?.DefaultReserveLimit ?? 0);
	}
}
