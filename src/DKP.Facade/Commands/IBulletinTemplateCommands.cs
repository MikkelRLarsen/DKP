using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBulletinTemplateCommands
{
    Task<BulletinTemplateDto> CreateAsync(CreateBulletinTemplateRequest request, CancellationToken cancellationToken = default);
    Task<BulletinTemplateDto> UpdateAsync(Guid templateId, UpdateBulletinTemplateRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid templateId, bool active, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid templateId, CancellationToken cancellationToken = default);
}
