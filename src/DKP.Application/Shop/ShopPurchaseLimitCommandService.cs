using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Application.Authentication;

namespace DKP.Application.Shop;

public sealed class ShopPurchaseLimitCommandService(CommandContext context, IShopPurchaseLimitRepository repository, TimeProvider time) : IShopPurchaseLimitCommands
{
    private static void Validate(ShopPurchaseLimitInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SharedKey) || input.SharedKey.Trim().Length > 64 ||
            input.MaxPerUser is <= 0 || input.MaxActivePerUser is <= 0 || input.MaxPerDay is <= 0 || input.MaxPerWeek is <= 0 || input.MaxPerMonth is <= 0)
            throw new ArgumentException("A shared key is required and all supplied limits must be positive.");
    }

    private static ShopPurchaseLimitDto Dto(ShopPurchaseLimit x) => new(x.Id, x.SharedKey, x.MaxPerUser, x.MaxActivePerUser, x.MaxPerDay, x.MaxPerWeek, x.MaxPerMonth, x.IsActive);

    public Task<ShopPurchaseLimitDto> CreateAsync(ShopPurchaseLimitInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var limit = new ShopPurchaseLimit(input.SharedKey.Trim(), input.MaxPerUser, input.MaxActivePerUser, input.MaxPerDay, input.MaxPerWeek, input.MaxPerMonth, time.GetUtcNow().UtcDateTime);
            await repository.AddAsync(limit, ct);
            return Dto(limit);
        }, ct);

    public Task<ShopPurchaseLimitDto> UpdateAsync(Guid id, ShopPurchaseLimitInput input, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            Validate(input);
            var limit = await repository.FindAsync(id, ct) ?? throw new KeyNotFoundException("Purchase limit not found.");
            limit.Update(input.SharedKey.Trim(), input.MaxPerUser, input.MaxActivePerUser, input.MaxPerDay, input.MaxPerWeek, input.MaxPerMonth, time.GetUtcNow().UtcDateTime);
            return Dto(limit);
        }, ct);

    public Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
        => context.ExecuteAsync([], true, async _ =>
        {
            var limit = await repository.FindAsync(id, ct) ?? throw new KeyNotFoundException("Purchase limit not found.");
            limit.SetActive(active, time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);
}
