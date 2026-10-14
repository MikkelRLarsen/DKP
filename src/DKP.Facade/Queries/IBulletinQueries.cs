using DKP.Facade.Contracts;
namespace DKP.Facade.Queries;
public interface IBulletinQueries
{
    Task<IReadOnlyList<BulletinItemDto>> GetAvailableItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BulletinClaimDto>> GetMyClaimsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BulletinItemDto>> GetAllItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BulletinClaimDto>> GetAllClaimsAsync(CancellationToken cancellationToken = default);
}
