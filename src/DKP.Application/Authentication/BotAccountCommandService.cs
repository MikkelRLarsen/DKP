using DKP.Application.Persistence;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.Authentication;

public sealed class BotAccountCommandService(
    IUserRepository users,
    IUserProvisioningService provisioning) : IBotAccountCommands
{
    public async Task<BotAccountDto> CreateAsync(string discordId, BotAccountInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(discordId)) throw new UnauthorizedAccessException("Discord user ID is required.");
        if (string.IsNullOrWhiteSpace(input.DiscordName) || input.DiscordName.Trim().Length > 128)
            throw new ArgumentException("Discord display name is required and must be at most 128 characters.");

        var existing = await users.FindByDiscordIdAsync(discordId, ct);
        var user = await provisioning.ProvisionAsync(new DiscordUserProfile(discordId, input.DiscordName.Trim(), input.AvatarUrl), ct);
        return new BotAccountDto(user.Id, user.DiscordId, user.DiscordName, ToRole(user.Role), existing is null);
    }

    private static UserRole ToRole(DKP.Domain.UserRole role) => role switch
    {
        DKP.Domain.UserRole.Member => UserRole.Member,
        DKP.Domain.UserRole.Officer => UserRole.Officer,
        _ => throw new InvalidOperationException("Unknown user role.")
    };
}
