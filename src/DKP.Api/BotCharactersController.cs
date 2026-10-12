using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DKP.Api;

[ApiController]
[Route("api/bot/characters")]
public sealed class BotCharactersController(
    IBotServiceAuthenticator authenticator,
    IBotCharacterQueries queries,
    IBotCharacterCommands commands,
    ILogger<BotCharactersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        var characters = await queries.GetAsync(discordId!, cancellationToken);
        return characters is null ? Unauthorized() : Ok(characters);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CharacterInput input, CancellationToken cancellationToken)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return Ok(await commands.CreateAsync(discordId!, input, cancellationToken)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpPut("{characterId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid characterId, [FromBody] CharacterInput input, CancellationToken cancellationToken)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try
        {
            var character = await commands.UpdateAsync(discordId!, characterId, input, cancellationToken);
            return character is null ? NotFound() : Ok(character);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpDelete("{characterId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid characterId, CancellationToken cancellationToken)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return await commands.DeleteAsync(discordId!, characterId, cancellationToken) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpPut("{characterId:guid}/main")]
    public async Task<IActionResult> SetMainAsync(Guid characterId, CancellationToken cancellationToken)
    {
        if (!TryGetDiscordId(out var discordId, out var failure)) return failure!;
        try { return await commands.SetMainCharacterAsync(discordId!, characterId, cancellationToken) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    private bool TryGetDiscordId(out string? discordId, out IActionResult? failure)
    {
        discordId = Request.Headers["X-DKP-Discord-User-Id"].FirstOrDefault();
        failure = null;
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        if (!authenticator.IsAuthenticated(Request))
        {
            logger.LogWarning("Rejected bot character request. CorrelationId: {CorrelationId}", correlationId);
            failure = Unauthorized();
            return false;
        }
        if (string.IsNullOrWhiteSpace(discordId))
        {
            logger.LogWarning("Rejected bot character request without Discord user ID. CorrelationId: {CorrelationId}", correlationId);
            failure = Unauthorized();
            return false;
        }
        return true;
    }
}
