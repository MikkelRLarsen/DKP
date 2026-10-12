using DKP.Application.Authentication;
using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
namespace DKP.Application.Users;

public sealed class UserRoleCommandService(CommandContext context, IUserRepository users, IOfficerIdentityPolicy bootstrap) : IUserRoleCommands
{
    public Task<bool> SetRoleAsync(SetUserRoleRequest request, CancellationToken ct = default)
        => context.ExecuteAsync([request.TargetUserId], true, async _ =>
        {
            if (!Enum.IsDefined(request.Role)) throw new ArgumentException("Invalid role.");
            var target = await users.FindByIdAsync(request.TargetUserId, ct) ?? throw new KeyNotFoundException("Player not found.");
            if (request.Role == UserRole.Member && bootstrap.IsOfficer(target.DiscordId))
                throw new InvalidOperationException("Configured bootstrap Officers cannot be demoted.");
            target.SetRole(request.Role switch
            {
                UserRole.Member => Domain.UserRole.Member,
                UserRole.Officer => Domain.UserRole.Officer,
                _ => throw new ArgumentException("Invalid role.")
            });
            return true;
        }, ct);
}
