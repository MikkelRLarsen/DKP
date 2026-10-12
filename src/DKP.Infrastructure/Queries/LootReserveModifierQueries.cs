using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class LootReserveModifierQueries(QuerySession session) : ILootReserveModifierQueries
{
    public Task<IReadOnlyList<LootReserveModifierDto>> GetActiveAsync(CancellationToken ct = default)
        => session.ReadAsync<IReadOnlyList<LootReserveModifierDto>>(true, async (db, _) =>
        {
            var events = await db.DkpEvents.AsNoTracking().ToArrayAsync(ct);
            var users = await db.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
            var result = new List<LootReserveModifierDto>();
            foreach (var group in events.GroupBy(x => x.UserId))
            {
                var state = LedgerReplayState.Replay(group);
                if (!users.TryGetValue(group.Key, out var user)) continue;
                result.AddRange(state.Modifiers.Values.Where(x => !x.IsRevoked && x.RemainingExports > 0).Select(x => new LootReserveModifierDto(x.ModifierId, group.Key, user.DiscordName, x.ModifierType == LootReserveModifierType.SoftReserve ? LootReserveModifierKind.SoftReserve : LootReserveModifierKind.RollBonus, x.Amount, x.RemainingExports, events.Where(e => e.UserId == group.Key && e.EventType == nameof(LootReserveModifierGranted)).Select(e => LedgerEvents.Read(e)).OfType<LootReserveModifierGranted>().Single(e => e.ModifierId == x.ModifierId).GrantReason, events.Where(e => e.UserId == group.Key && e.EventType == nameof(LootReserveModifierGranted)).Single(e => ((LootReserveModifierGranted)LedgerEvents.Read(e)).ModifierId == x.ModifierId).OccurredAtUtc)));
            }
            return result.OrderBy(x => x.DiscordName).ThenBy(x => x.CreatedAtUtc).ToArray();
        }, ct);
}
