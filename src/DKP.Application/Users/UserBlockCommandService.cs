using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.Users;

public sealed class UserBlockCommandService(CommandContext context, IUserRepository users, IOfficerIdentityPolicy bootstrap, TimeProvider time) : IUserBlockCommands
{
    public Task<bool> BlockAsync(BlockUserRequest request, CancellationToken ct = default)
        => context.ExecuteAsync([request.TargetUserId], true, async actor =>
        {
            var target = await users.FindByIdAsync(request.TargetUserId, ct) ?? throw new KeyNotFoundException("Player not found.");
            if (bootstrap.IsOfficer(target.DiscordId)) throw new InvalidOperationException("Configured bootstrap Officers cannot be blocked.");
            var reason = request.Reason?.Trim();
            if (reason?.Length > 500) throw new ArgumentException("Block reason must be at most 500 characters.");
            target.Block(actor.Id, string.IsNullOrEmpty(reason) ? null : reason, time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);
    public Task<bool> UnblockAsync(Guid targetUserId, CancellationToken ct = default)
        => context.ExecuteAsync([targetUserId], true, async _ =>
        {
            var target = await users.FindByIdAsync(targetUserId, ct) ?? throw new KeyNotFoundException("Player not found.");
            target.Unblock();
            return true;
        }, ct);
}
