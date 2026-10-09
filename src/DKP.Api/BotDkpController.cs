using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/dkp")]
public sealed class BotDkpController(
    IBotServiceAuthenticator authenticator,
    IBotDkpQueries queries,
    ILogger<BotDkpController> logger) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetBalanceAsync(CancellationToken cancellationToken)
        => GetHistoryAsync(null, cancellationToken);

    [HttpGet("history")]
    public Task<IActionResult> GetHistoryAsync([FromQuery] int? limit, CancellationToken cancellationToken)
        => ExecuteAsync(limit, cancellationToken);

    private async Task<IActionResult> ExecuteAsync(int? limit, CancellationToken cancellationToken)
    {
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        if (!authenticator.IsAuthenticated(Request))
        {
            logger.LogWarning("Rejected bot DKP request. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized();
        }

        var discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(discordId))
        {
            logger.LogWarning("Rejected bot DKP request without Discord user ID. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized();
        }

        if (limit is < 1 or > 50)
            return BadRequest(new { error = "limit must be between 1 and 50." });

        var history = await queries.GetHistoryAsync(discordId, limit, cancellationToken);
        return history is null ? Unauthorized() : Ok(history);
    }
}
