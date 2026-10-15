using DKP.Facade.Contracts;

namespace DKP.Facade.Commands;

public interface IBotAwardRequestReviewCommands
{
    Task<bool> ApproveAsync(string discordId, Guid requestId, string? comment, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(string discordId, Guid requestId, string? comment, CancellationToken cancellationToken = default);
}
