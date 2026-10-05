using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Contracts;
namespace DKP.Application.Authentication;

/// <summary>Every command revalidates the trusted actor and targets inside a fresh transaction.</summary>
public sealed class CommandContext(ICurrentUser currentUser, IUserRepository users, ICommandUnitOfWork unitOfWork)
{
    public async Task<T> ExecuteAsync<T>(IReadOnlyCollection<Guid> userIds, bool officerOnly, Func<User, Task<T>> action, CancellationToken ct)
    {
        var discordId = await currentUser.GetDiscordIdAsync(ct);
        if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("Login is required.");
        return await unitOfWork.ExecuteAsync(userIds, async () =>
        {
            var actor = await users.FindByDiscordIdAsync(discordId, ct);
            if (actor is null || actor.IsBlocked || (officerOnly && actor.Role != Domain.UserRole.Officer))
                throw new UnauthorizedAccessException("An active " + (officerOnly ? "Officer" : "member") + " is required.");
            return await action(actor);
        }, ct);
    }
    public async Task<User> TargetAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct) ?? throw new KeyNotFoundException($"Player {userId} does not exist.");
        if (user.IsBlocked) throw new InvalidOperationException($"{user.DiscordName} is blocked.");
        return user;
    }
    public static Guid[] Targets(IReadOnlyCollection<Guid> ids)
    {
        if (ids is null || ids.Count == 0 || ids.Contains(Guid.Empty)) throw new ArgumentException("Select at least one player.");
        return ids.Distinct().Order().ToArray();
    }
}
