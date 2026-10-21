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
    IDiscordForumSectionCommands commands,
    IDiscordForumProvisionQueries provisionQueries,
    IDiscordForumProvisionCommands provisionCommands) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await queries.GetAllForBotAsync(cancellationToken));

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDiscordForumSectionRequest request, CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await commands.RegisterAsync(request, cancellationToken));

    [HttpGet("provision-requests/pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await provisionQueries.GetPendingForBotAsync(cancellationToken));

    [HttpPost("provision-requests/{requestId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid requestId, CompleteDiscordForumProvisionRequest request, CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await provisionCommands.CompleteAsync(requestId, request.ForumChannelId, request.ThreadId, request.MessageId, cancellationToken));

    [HttpPost("provision-requests/{requestId:guid}/failed")]
    public async Task<IActionResult> Failed(Guid requestId, FailDiscordForumProvisionRequest request, CancellationToken cancellationToken)
        => !authenticator.IsAuthenticated(Request) ? Unauthorized() : Ok(await provisionCommands.FailAsync(requestId, request.Error, cancellationToken));
}
