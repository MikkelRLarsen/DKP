using DKP.Domain;
namespace DKP.Application.Persistence;
public interface IEventLedgerRepository
{
    Task<LedgerReplayState> GetStateAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<DkpEvent> PostAsync(Guid userId, Guid actorId, Guid operationId, DateTime now, ILedgerPayload payload, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DkpEvent>> GetAllEventsAsync(CancellationToken cancellationToken = default);
}
