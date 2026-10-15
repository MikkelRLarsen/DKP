using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BulletinTemplateQueries(QuerySession session) : IBulletinTemplateQueries
{
    public Task<IReadOnlyList<BulletinTemplateDto>> GetAllAsync(CancellationToken ct = default)
        => session.ReadAsync(true, async (db, _) => (IReadOnlyList<BulletinTemplateDto>)await db.BulletinTemplates.AsNoTracking().OrderByDescending(x => x.IsActive).ThenBy(x => x.Name).Select(x => new BulletinTemplateDto(x.Id, x.Name, x.Title, x.Description, x.DkpAmount, x.MaxCompletions, x.UniquePerUser, x.DefaultDeadlineOffset, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc)).ToArrayAsync(ct), ct);

    public Task<IReadOnlyList<BulletinTemplateDto>> GetActiveAsync(CancellationToken ct = default)
        => session.ReadAsync(true, async (db, _) => (IReadOnlyList<BulletinTemplateDto>)await db.BulletinTemplates.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new BulletinTemplateDto(x.Id, x.Name, x.Title, x.Description, x.DkpAmount, x.MaxCompletions, x.UniquePerUser, x.DefaultDeadlineOffset, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc)).ToArrayAsync(ct), ct);
}
