using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class DkpAwardRequestRepository(CommandUnitOfWork session) : IDkpAwardRequestRepository
{
    public Task<DkpAwardRequest?> FindAsync(Guid id, CancellationToken ct = default) => session.Db.DkpAwardRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> HasPendingAsync(Guid userId, Guid presetId, CancellationToken ct = default) => session.Db.DkpAwardRequests.AnyAsync(x => x.UserId == userId && x.PresetId == presetId && x.Status == DkpAwardRequestStatus.Pending, ct);
    public Task AddAsync(DkpAwardRequest request, CancellationToken ct = default) { session.Db.DkpAwardRequests.Add(request); return Task.CompletedTask; }
}
