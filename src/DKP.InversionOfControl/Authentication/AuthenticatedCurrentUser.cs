using System.Security.Claims;
using DKP.Facade.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
namespace DKP.InversionOfControl.Authentication;

/// <summary>Uses the server-authenticated circuit principal, not HTTP context left over from connection setup.</summary>
public sealed class AuthenticatedCurrentUser(AuthenticationStateProvider stateProvider) : ICurrentUser
{
    public async Task<string?> GetDiscordIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = await stateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true ? state.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    }
}
