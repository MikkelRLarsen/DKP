using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/dkp")]
public sealed class BotDkpRequestController(
    IBotServiceAuthenticator authenticator,
    IBotDkpPresetQueries queries,
    IBotDkpRequestCommands commands,
    ILogger<BotDkpRequestController> logger) : ControllerBase
{
    [HttpGet("sources")]
    public async Task<IActionResult> SourcesAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var sources = await queries.GetAvailableSourcesAsync(discordId!, ct);
        return sources is null ? Unauthorized() : Ok(sources);
    }

    [HttpPost("requests")]
    public Task<IActionResult> CreateAsync([FromBody] BotDkpRequestInput input, CancellationToken ct)
        => CreateCoreAsync(input, ct);

    [HttpPost("requests/multi")]
    public Task<IActionResult> CreateMultiAsync([FromBody] BotDkpRequestInput input, CancellationToken ct)
        => CreateCoreAsync(input, ct);

    [HttpDelete("requests/{requestId:guid}")]
    public async Task<IActionResult> CancelAsync(Guid requestId, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return await commands.CancelAsync(discordId!, requestId, ct) ? NoContent() : NotFound(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    private async Task<IActionResult> CreateCoreAsync(BotDkpRequestInput input, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return Ok(await commands.CreateAsync(discordId!, input.PresetId, input.Quantity, input.TargetDiscordIds, input.Comment, ct)); }
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
            logger.LogWarning("Rejected bot DKP request. CorrelationId: {CorrelationId}", correlationId);
            failure = Unauthorized();
            return false;
        }
        return true;
    }
}

public sealed record BotDkpRequestInput(Guid PresetId, int Quantity, IReadOnlyList<string>? TargetDiscordIds, string? Comment);
