using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Bulletins;

public sealed class BulletinTemplateCommandService(CommandContext context, IBulletinTemplateRepository repository, TimeProvider time) : IBulletinTemplateCommands
{
    public Task<BulletinTemplateDto> CreateAsync(CreateBulletinTemplateRequest input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var now = time.GetUtcNow().UtcDateTime;
            var template = new BulletinTemplate(input.Name, input.Title, input.Description, input.DkpAmount, input.MaxCompletions, input.UniquePerUser, input.DefaultDeadlineOffset, now);
            await repository.AddAsync(template, ct);
            return ToDto(template);
        }, ct);

    public Task<BulletinTemplateDto> UpdateAsync(Guid templateId, UpdateBulletinTemplateRequest input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var template = await repository.FindAsync(templateId, ct) ?? throw new KeyNotFoundException("Bulletin template not found.");
            template.Update(input.Name, input.Title, input.Description, input.DkpAmount, input.MaxCompletions, input.UniquePerUser, input.DefaultDeadlineOffset, time.GetUtcNow().UtcDateTime);
            return ToDto(template);
        }, ct);

    public Task SetActiveAsync(Guid templateId, bool active, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var template = await repository.FindAsync(templateId, ct) ?? throw new KeyNotFoundException("Bulletin template not found.");
            template.SetActive(active, time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);

    public Task ArchiveAsync(Guid templateId, CancellationToken ct = default) => SetActiveAsync(templateId, false, ct);

    private static BulletinTemplateDto ToDto(BulletinTemplate template) => new(template.Id, template.Name, template.Title, template.Description, template.DkpAmount, template.MaxCompletions, template.UniquePerUser, template.DefaultDeadlineOffset, template.IsActive, template.CreatedAtUtc, template.UpdatedAtUtc);
}
