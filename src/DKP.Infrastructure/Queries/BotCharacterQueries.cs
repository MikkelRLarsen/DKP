using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DKP.Infrastructure.Queries;

public sealed class BotCharacterQueries(IDbContextFactory<DkpDbContext> factory) : IBotCharacterQueries
{
    public async Task<IReadOnlyList<CharacterDto>?> GetAsync(string discordId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return null;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, cancellationToken);
        if (user is null || user.IsBlocked) return null;

        return await db.Characters.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.Id)
            .Select(x => new CharacterDto(x.Id, x.FirstName, x.LastName, x.IsMain))
            .ToArrayAsync(cancellationToken);
    }
}
