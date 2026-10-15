using DKP.Domain;

namespace DKP.Application.Persistence;

public interface IBulletinTemplateRepository
{
    Task<BulletinTemplate?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(BulletinTemplate template, CancellationToken cancellationToken = default);
}
