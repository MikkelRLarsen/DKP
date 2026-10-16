using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DKP.Api;

[ApiController]
[Route("api/bot/forum-sections")]
public sealed class BotForumSectionsController(
    IBotServiceAuthenticator authenticator,
    IDiscordForumSectionQueries queries,
    IDiscordForumSectionCommands commands) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await queries.GetAllForBotAsync(cancellationToken));

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDiscordForumSectionRequest request, CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await commands.RegisterAsync(request, cancellationToken));
}
