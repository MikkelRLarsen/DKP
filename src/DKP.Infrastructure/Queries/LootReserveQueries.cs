using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class LootReserveQueries(QuerySession session) : ILootReserveQueries
{
    public Task<LootReserveSettingsDto> GetSettingsAsync(CancellationToken ct = default)
        => session.ReadAsync(true, async (db, _) => new LootReserveSettingsDto(await db.GuildSettings.Select(x => x.DefaultReserveLimit).SingleAsync(ct)), ct);
    public Task<IReadOnlyList<LootReserveMemberDto>> GetMembersAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<LootReserveMemberDto>>(true, async (db, _) =>
        {
            var defaultLimit = await db.GuildSettings.Select(x => x.DefaultReserveLimit).SingleAsync(ct);
            return await db.Users.Where(u => !u.IsBlocked).OrderBy(u => u.DiscordName).ThenBy(u => u.Id)
                .Select(u => new LootReserveMemberDto(u.Id, u.DiscordName,
                    u.Characters.OrderByDescending(c => c.IsMain).ThenBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
                        .Select(c => new CharacterDto(c.Id, c.FirstName, c.LastName, c.IsMain)).ToList(),
                    defaultLimit + db.ShopPurchaseProjections.Where(p => p.UserId == u.Id && p.CancelledAtUtc == null && p.ItemKey == "soft-reserve").Sum(p => p.Quantity),
                    db.ShopPurchaseProjections.Where(p => p.UserId == u.Id && p.CancelledAtUtc == null).Sum(p => p.RollBonusValue ?? 0),
                    u.Characters.Any())).ToArrayAsync(ct);
        }, ct);
}
