using DKP.Facade.Commands;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DKP.Api;

[ApiController]
[Route("api/bot/requests")]
public sealed class BotAwardRequestController(
    IBotServiceAuthenticator authenticator,
    IBotAwardRequestQueries queries,
    IBotAwardRequestReviewCommands commands) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> PendingAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var result = await queries.GetPendingAsync(discordId!, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("{requestId:guid}/approve")]
    public Task<IActionResult> ApproveAsync(Guid requestId, [FromBody] ReviewInput? input, CancellationToken ct)
        => ReviewAsync(requestId, input?.Comment, true, ct);

    [HttpPost("{requestId:guid}/reject")]
    public Task<IActionResult> RejectAsync(Guid requestId, [FromBody] ReviewInput? input, CancellationToken ct)
        => ReviewAsync(requestId, input?.Comment, false, ct);

    private async Task<IActionResult> ReviewAsync(Guid requestId, string? comment, bool approve, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try
        {
            var succeeded = approve
                ? await commands.ApproveAsync(discordId!, requestId, comment, ct)
                : await commands.RejectAsync(discordId!, requestId, comment, ct);
            return succeeded ? Ok(new { success = true }) : NotFound();
        }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    private bool TryGetDiscordId(out string? discordId, out IActionResult? failure)
    {
        discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        failure = null;
        if (!authenticator.IsAuthenticated(Request) || string.IsNullOrWhiteSpace(discordId))
        {
            failure = Unauthorized();
            return false;
        }
        return true;
    }
}

public sealed record ReviewInput(string? Comment);
