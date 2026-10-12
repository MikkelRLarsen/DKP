using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot")]
public sealed class BotHealthController(
    IBotServiceAuthenticator authenticator,
    ILogger<BotHealthController> logger) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        if (!authenticator.IsAuthenticated(Request))
        {
            logger.LogWarning("Rejected bot health request. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized();
        }

        return Ok(new BotHealthResponse("ok", "dkp-api", correlationId));
    }
}
