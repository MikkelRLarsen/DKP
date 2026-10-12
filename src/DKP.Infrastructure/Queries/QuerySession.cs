using System.Data;
using DKP.Domain;
using DKP.Facade.Contracts;
using DKP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace DKP.Infrastructure.Queries;

/// <summary>A consistent read snapshot and a freshly validated authenticated actor.</summary>
public sealed class QuerySession(IDbContextFactory<DkpDbContext> factory, ICurrentUser currentUser)
{
    public async Task<T> ReadAsync<T>(bool officerOnly, Func<DkpDbContext, User, Task<T>> read, CancellationToken ct)
    {
        var discordId = await currentUser.GetDiscordIdAsync(ct);
        if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("Login is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var actor = await db.Users.SingleOrDefaultAsync(x => x.DiscordId == discordId, ct);
        if (actor == null || actor.IsBlocked || (officerOnly && actor.Role != Domain.UserRole.Officer))
            throw new UnauthorizedAccessException("An active " + (officerOnly ? "Officer" : "member") + " is required.");
        return await read(db, actor);
    }
}
