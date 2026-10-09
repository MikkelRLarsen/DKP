using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/achievements")]
public sealed class BotAchievementsController(
    IBotServiceAuthenticator authenticator,
    IBotAchievementQueries queries,
    IBotAchievementRequestCommands commands,
    ILogger<BotAchievementsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var result = await queries.GetOverviewAsync(discordId!, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequestAsync([FromBody] AchievementRequestInput input, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return Ok(await commands.CreateAsync(discordId!, input.AchievementId, input.Comment, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    [HttpDelete("requests/{requestId:guid}")]
    public async Task<IActionResult> CancelRequestAsync(Guid requestId, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return await commands.CancelAsync(discordId!, requestId, ct) ? NoContent() : NotFound(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    [HttpPost("requests/multi")]
    public async Task<IActionResult> CreateMultiRequestAsync([FromBody] MultiAchievementRequestInput input, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try
        {
            var request = new BotMultiAchievementRequest(input.AchievementId, input.TargetDiscordIds ?? [], input.Comment);
            return Ok(await commands.CreateForUsersAsync(discordId!, request, ct));
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
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        if (!authenticator.IsAuthenticated(Request) || string.IsNullOrWhiteSpace(discordId))
        {
            logger.LogWarning("Rejected bot achievement request. CorrelationId: {CorrelationId}", correlationId);
            failure = Unauthorized();
            return false;
        }
        return true;
    }
}

public sealed record AchievementRequestInput(Guid AchievementId, string? Comment);
public sealed record MultiAchievementRequestInput(Guid AchievementId, IReadOnlyList<string>? TargetDiscordIds, string? Comment);
