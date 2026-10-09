using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotShopQueries(IDbContextFactory<DkpDbContext> factory) : IBotShopQueries
{
    public async Task<IReadOnlyList<ShopItemDto>?> GetActiveItemsAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        return await ReadModels.ItemsAsync(db, true, ct);
    }

    public async Task<IReadOnlyList<ShopPurchaseDto>?> GetPurchasesAsync(string discordId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (user is null || user.IsBlocked) return null;
        return await ReadModels.PurchasesAsync(db, user.Id, ct);
    }
}
