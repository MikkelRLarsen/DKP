using DKP.Facade.Commands;
using Microsoft.AspNetCore.Mvc;

namespace DKP.Api;

[ApiController]
[Route("api/bot/notifications/preferences")]
public sealed class BotNotificationPreferenceController(
    IBotServiceAuthenticator authenticator,
    IBotNotificationPreferenceCommands commands) : ControllerBase
{
    [HttpPost("review-toggle")]
    public async Task<IActionResult> ToggleReviewNotificationsAsync(CancellationToken ct)
    {
        var discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        if (!authenticator.IsAuthenticated(Request) || string.IsNullOrWhiteSpace(discordId))
            return Unauthorized();

        try
        {
            return Ok(await commands.ToggleReviewNotificationsAsync(discordId, ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
