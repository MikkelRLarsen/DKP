using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IDkpAwardRequestQueries
{
    Task<IReadOnlyList<DkpAwardRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DkpAwardRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DkpAwardRequestDto>> GetAllRequestsAsync(CancellationToken cancellationToken = default);
    Task<DkpAwardRequestDto?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
}
