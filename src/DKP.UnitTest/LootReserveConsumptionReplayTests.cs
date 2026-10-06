using DKP.Domain;
using Xunit;

namespace DKP.UnitTest;

public sealed class LootReserveConsumptionReplayTests
{
    [Fact]
    public void Consume_and_revert_restore_the_same_active_purchase_state()
    {
        var user = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var batch = Guid.NewGuid();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var purchase = LedgerEvents.Create(user, actor, 1, Guid.NewGuid(), now,
            new PurchasePlaced(purchaseId, itemId, "soft-reserve", "Soft Reserve", 2, 10, null));
        var consumed = LedgerEvents.Create(user, actor, 2, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveConsumed(batch, 2, null, [purchaseId]));
        var reverted = LedgerEvents.Create(user, actor, 3, Guid.NewGuid(), now.AddSeconds(2),
            new LootReserveConsumptionReverted(batch, consumed.Id));

        var consumedState = LedgerReplayState.Replay([purchase, consumed]);
        Assert.Equal(0, consumedState.ActiveQuantity(itemId));
        Assert.True(consumedState.Purchases[purchaseId].IsConsumed);

        var restoredState = LedgerReplayState.Replay([purchase, consumed, reverted]);
        Assert.Equal(2, restoredState.ActiveQuantity(itemId));
        Assert.False(restoredState.Purchases[purchaseId].IsConsumed);
    }

    [Fact]
    public void A_consumed_purchase_cannot_be_consumed_twice_without_revert()
    {
        var user = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var purchase = LedgerEvents.Create(user, actor, 1, Guid.NewGuid(), now,
            new PurchasePlaced(purchaseId, Guid.NewGuid(), "soft-reserve", "Soft Reserve", 1, 10, null));
        var first = LedgerEvents.Create(user, actor, 2, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveConsumed(Guid.NewGuid(), 1, null, [purchaseId]));
        var second = LedgerEvents.Create(user, actor, 3, Guid.NewGuid(), now.AddSeconds(2),
            new LootReserveConsumed(Guid.NewGuid(), 1, null, [purchaseId]));

        Assert.Throws<InvalidOperationException>(() => LedgerReplayState.Replay([purchase, first, second]));
    }

    [Fact]
    public void Modifier_is_consumed_with_batch_and_restored_by_revert()
    {
        var user = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var batch = Guid.NewGuid();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var purchase = LedgerEvents.Create(user, actor, 1, Guid.NewGuid(), now,
            new PurchasePlaced(purchaseId, itemId, "soft-reserve", "Soft Reserve", 1, 10, null));
        var grant = LedgerEvents.Create(user, actor, 2, Guid.NewGuid(), now.AddMilliseconds(1),
            new LootReserveModifierGranted(modifierId, LootReserveModifierType.SoftReserve, 1, 3, "Missed raid"));
        var consume = LedgerEvents.Create(user, actor, 3, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveConsumed(batch, 1, null, [purchaseId]));
        var modifierConsume = LedgerEvents.Create(user, actor, 4, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveModifierConsumed(modifierId, batch, 1));
        var revert = LedgerEvents.Create(user, actor, 5, Guid.NewGuid(), now.AddSeconds(2),
            new LootReserveConsumptionReverted(batch, consume.Id));

        var consumed = LedgerReplayState.Replay([purchase, grant, consume, modifierConsume]);
        Assert.Equal(2, consumed.Modifiers[modifierId].RemainingExports);
        Assert.Equal(1, consumed.ActiveModifierAmount(LootReserveModifierType.SoftReserve));

        var restored = LedgerReplayState.Replay([purchase, grant, consume, modifierConsume, revert]);
        Assert.Equal(3, restored.Modifiers[modifierId].RemainingExports);
    }

    [Fact]
    public void Modifier_only_batch_is_valid_for_a_player_without_purchases()
    {
        var user = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var batch = Guid.NewGuid();
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var grant = LedgerEvents.Create(user, actor, 1, Guid.NewGuid(), now,
            new LootReserveModifierGranted(modifierId, LootReserveModifierType.SoftReserve, 1, 2, "Penalty"));
        var anchor = LedgerEvents.Create(user, actor, 2, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveConsumed(batch, 0, null, []));
        var modifierConsume = LedgerEvents.Create(user, actor, 3, Guid.NewGuid(), now.AddSeconds(1),
            new LootReserveModifierConsumed(modifierId, batch, 1));

        var state = LedgerReplayState.Replay([grant, anchor, modifierConsume]);

        Assert.Equal(1, state.Modifiers[modifierId].RemainingExports);
    }
}
