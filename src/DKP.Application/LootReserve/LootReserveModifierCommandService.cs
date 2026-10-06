using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.LootReserve;

public sealed class LootReserveModifierCommandService(CommandContext context, IEventLedgerRepository ledger, TimeProvider time) : ILootReserveModifierCommands
{
    public Task<IReadOnlyList<LootReserveModifierDto>> GrantAsync(IReadOnlyCollection<Guid> userIds, CreateLootReserveModifierRequest request, CancellationToken ct = default)
    {
        var ids = CommandContext.Targets(userIds);
        if (request.Amount <= 0) throw new ArgumentException("Modifier amount must be greater than zero.");
        if (request.ExportCount <= 0) throw new ArgumentException("Export count must be greater than zero.");
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) throw new ArgumentException("A reason between 1 and 500 characters is required.");
        return context.ExecuteAsync(ids, true, async officer =>
        {
            var now = time.GetUtcNow().UtcDateTime;
            var operation = Guid.NewGuid();
            var result = new List<LootReserveModifierDto>();
            foreach (var id in ids)
            {
                var user = await context.TargetAsync(id, ct);
                var modifierId = Guid.NewGuid();
                var payload = new LootReserveModifierGranted(modifierId, request.Kind switch
                {
                    LootReserveModifierKind.SoftReserve => DKP.Domain.LootReserveModifierType.SoftReserve,
                    LootReserveModifierKind.RollBonus => DKP.Domain.LootReserveModifierType.RollBonus,
                    _ => throw new ArgumentOutOfRangeException(nameof(request.Kind))
                }, request.Amount, request.ExportCount, reason!);
                await ledger.PostAsync(id, officer.Id, operation, now, payload, ct);
                result.Add(new LootReserveModifierDto(modifierId, id, user.DiscordName, request.Kind, request.Amount, request.ExportCount, reason!, now));
            }
            return (IReadOnlyList<LootReserveModifierDto>)result;
        }, ct);
    }

    public Task RevokeAsync(Guid modifierId, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async officer =>
        {
            var events = await ledger.GetAllEventsAsync(ct);
            var grant = events.FirstOrDefault(x => x.EventType == nameof(LootReserveModifierGranted) && ((LootReserveModifierGranted)LedgerEvents.Read(x)).ModifierId == modifierId)
                ?? throw new KeyNotFoundException("Modifier was not found.");
            var state = LedgerReplayState.Replay(events.Where(x => x.UserId == grant.UserId));
            if (!state.Modifiers.TryGetValue(modifierId, out var modifier) || modifier.IsRevoked)
                throw new InvalidOperationException("Modifier is already revoked or inactive.");
            await ledger.PostAsync(grant.UserId, officer.Id, Guid.NewGuid(), time.GetUtcNow().UtcDateTime, new LootReserveModifierRevoked(modifierId, "Revoked by Officer"), ct);
            return true;
        }, ct);
}
