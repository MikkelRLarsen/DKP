using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/shop")]
public sealed class BotShopController(
    IBotServiceAuthenticator authenticator,
    IBotShopQueries queries,
    IBotShopCommands commands,
    ILogger<BotShopController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetItemsAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var items = await queries.GetActiveItemsAsync(discordId!, ct);
        return items is null ? Unauthorized() : Ok(items);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchasesAsync(CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var purchases = await queries.GetPurchasesAsync(discordId!, ct);
        return purchases is null ? Unauthorized() : Ok(purchases);
    }

    [HttpPost("purchases")]
    public async Task<IActionResult> PurchaseAsync([FromBody] ShopPurchaseRequest request, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return Ok(await commands.PurchaseAsync(discordId!, request, ct)); }
        catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
    }

    [HttpDelete("purchases/{purchaseId:guid}")]
    public async Task<IActionResult> CancelAsync(Guid purchaseId, CancellationToken ct)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return await commands.CancelAsync(discordId!, purchaseId, ct) ? NoContent() : NotFound(); }
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
            logger.LogWarning("Rejected bot shop request. CorrelationId: {CorrelationId}", correlationId);
            failure = Unauthorized();
            return false;
        }
        return true;
    }
}
