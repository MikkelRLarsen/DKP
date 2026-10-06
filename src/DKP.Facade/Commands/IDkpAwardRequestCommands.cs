using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IDkpAwardRequestCommands
{
    Task<DkpAwardRequestDto> CreateAsync(CreateDkpAwardRequestRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid requestId, ReviewDkpAwardRequestRequest request, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid requestId, ReviewDkpAwardRequestRequest request, CancellationToken cancellationToken = default);
}
