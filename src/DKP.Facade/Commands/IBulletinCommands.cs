using DKP.Facade.Contracts;
namespace DKP.Facade.Commands;
public interface IBulletinCommands
{
    Task<BulletinItemDto> CreateItemAsync(CreateBulletinItemRequest request, CancellationToken cancellationToken = default);
    Task<BulletinItemDto> UpdateItemAsync(Guid itemId, UpdateBulletinItemRequest request, CancellationToken cancellationToken = default);
    Task SetItemActiveAsync(Guid itemId, bool isActive, CancellationToken cancellationToken = default);
    Task SetItemHiddenAsync(Guid itemId, bool isHidden, CancellationToken cancellationToken = default);
    Task<BulletinClaimDto> ClaimAsync(CreateBulletinClaimRequest request, CancellationToken cancellationToken = default);
    Task CancelClaimAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task ApproveClaimAsync(Guid claimId, ReviewBulletinClaimRequest request, CancellationToken cancellationToken = default);
    Task RejectClaimAsync(Guid claimId, ReviewBulletinClaimRequest request, CancellationToken cancellationToken = default);
    Task RevertApprovedClaimAsync(Guid claimId, ReviewBulletinClaimRequest request, CancellationToken cancellationToken = default);
}
