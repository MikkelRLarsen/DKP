using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/account")]
public sealed class BotAccountController(
    IBotServiceAuthenticator authenticator,
    IBotAccountCommands commands,
    ILogger<BotAccountController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] BotAccountInput input, CancellationToken ct)
    {
        var discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        if (!authenticator.IsAuthenticated(Request) || string.IsNullOrWhiteSpace(discordId))
        {
            logger.LogWarning("Rejected bot account request. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized();
        }

        try { return Ok(await commands.CreateAsync(discordId!, input, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }
}
