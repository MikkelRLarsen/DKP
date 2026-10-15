using DKP.Facade.Commands;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DKP.Api;

[ApiController]
[Route("api/bot/bulletin")]
public sealed class BotBulletinController(IBotServiceAuthenticator authenticator, IBotBulletinQueries queries, IBotBulletinCommands commands) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var items = await queries.GetAvailableItemsAsync(discordId!, ct);
        return items is null ? Unauthorized() : Ok(items);
    }

    [HttpPost("claims")]
    public async Task<IActionResult> ClaimAsync([FromBody] BotBulletinClaimRequest request, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return Ok(await commands.ClaimAsync(discordId!, request.BulletinItemId, request.Quantity, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    [HttpGet("claims/pending")]
    public async Task<IActionResult> GetPendingClaimsAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var claims = await queries.GetPendingClaimsAsync(discordId!, ct);
        return claims is null ? Unauthorized() : Ok(claims);
    }

    [HttpPost("claims/{claimId:guid}/approve")]
    public async Task<IActionResult> ApproveAsync(Guid claimId, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try
        {
            await commands.ApproveAsync(discordId!, claimId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    [HttpPost("claims/{claimId:guid}/deny")]
    public async Task<IActionResult> DenyAsync(Guid claimId, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try
        {
            await commands.RejectAsync(discordId!, claimId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    private bool TryGetDiscordId(out string? discordId, out IActionResult? failure)
    {
        discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        failure = null;
        if (!authenticator.IsAuthenticated(Request) || string.IsNullOrWhiteSpace(discordId)) { failure = Unauthorized(); return false; }
        return true;
    }
}

public sealed record BotBulletinClaimRequest(Guid BulletinItemId, int Quantity);
