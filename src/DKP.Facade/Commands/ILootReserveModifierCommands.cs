using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface ILootReserveModifierCommands
{
    Task<IReadOnlyList<LootReserveModifierDto>> GrantAsync(IReadOnlyCollection<Guid> userIds, CreateLootReserveModifierRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid modifierId, CancellationToken cancellationToken = default);
}
