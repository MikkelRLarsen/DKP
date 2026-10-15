using DKP.Facade.Contracts;

namespace DKP.Facade.Queries;

public interface IBulletinTemplateQueries
{
    Task<IReadOnlyList<BulletinTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BulletinTemplateDto>> GetActiveAsync(CancellationToken cancellationToken = default);
}
