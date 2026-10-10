using DKP.Facade.Commands;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/notifications")]
public sealed class BotNotificationsController(
    IBotServiceAuthenticator authenticator,
    IBotNotificationQueries queries,
    IBotNotificationCommands commands,
    ILogger<BotNotificationsController> logger) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> PendingAsync([FromQuery] int limit, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        return Ok(await queries.ClaimPendingAsync(limit <= 0 ? 10 : limit, ct));
    }

    [HttpPost("{id:guid}/sent")]
    public async Task<IActionResult> SentAsync(Guid id, [FromBody] NotificationSentInput input, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        await commands.MarkSentAsync(id, input.MessageId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deleted")]
    public async Task<IActionResult> DeletedAsync(Guid id, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        await commands.MarkDeletedAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/failed")]
    public async Task<IActionResult> FailedAsync(Guid id, [FromBody] NotificationFailureInput input, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        await commands.MarkFailedAsync(id, input.Error, ct);
        return NoContent();
    }

    private bool Authorized()
    {
        if (authenticator.IsAuthenticated(Request)) return true;
        logger.LogWarning("Rejected bot notification request.");
        return false;
    }
}

public sealed record NotificationFailureInput(string? Error);
public sealed record NotificationSentInput(ulong MessageId);
