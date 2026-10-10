using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;
public sealed class LootReserveQueries(QuerySession session) : ILootReserveQueries
{
    public Task<IReadOnlyList<LootReserveMemberDto>> GetMembersAsync(CancellationToken ct = default) => session.ReadAsync<IReadOnlyList<LootReserveMemberDto>>(true, async (db, _) =>
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Characters).Where(u => !u.IsBlocked).OrderBy(u => u.DiscordName).ThenBy(u => u.Id).ToArrayAsync(ct);
        var events = await db.DkpEvents.AsNoTracking().ToArrayAsync(ct);
        var states = events.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => DKP.Domain.LedgerReplayState.Replay(x));
        return users.Select(u => { var state = states.GetValueOrDefault(u.Id); var active = state?.Purchases.Values.Where(p => p.CancelledAtUtc is null && !p.IsConsumed).ToArray() ?? []; var softReserve = active.Where(p => p.ItemKey == "soft-reserve").Sum(p => p.Quantity); var rollBonus = active.Sum(p => p.RollBonusValue ?? 0); var softPenalty = state?.ActiveModifierAmount(LootReserveModifierType.SoftReserve) ?? 0; var rollPenalty = state?.ActiveModifierAmount(LootReserveModifierType.RollBonus) ?? 0; var extraReserve = softReserve - softPenalty; return new LootReserveMemberDto(u.Id, u.DiscordName, u.Characters.OrderByDescending(c => c.IsMain).ThenBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id).Select(c => new CharacterDto(c.Id, c.FirstName, c.LastName, c.IsMain)).ToArray(), extraReserve, Math.Max(0, rollBonus - rollPenalty), u.Characters.Count > 0); }).ToArray();
    }, ct);
}
