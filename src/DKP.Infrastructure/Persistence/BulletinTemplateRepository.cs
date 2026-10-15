using DKP.Application.Persistence;
using DKP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Persistence;

public sealed class BulletinTemplateRepository(CommandUnitOfWork unit) : IBulletinTemplateRepository
{
    public Task<BulletinTemplate?> FindAsync(Guid id, CancellationToken ct = default) => unit.Db.BulletinTemplates.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(BulletinTemplate template, CancellationToken ct = default) { unit.Db.BulletinTemplates.Add(template); return Task.CompletedTask; }
}
