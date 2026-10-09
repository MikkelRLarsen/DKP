using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpAwardRequests;

public sealed class BotAchievementRequestCommandService(
    IUserRepository users,
    IDkpAwardRequestRepository requests,
    IAchievementRepository achievements,
    ICommandUnitOfWork unitOfWork,
    TimeProvider time) : IBotAchievementRequestCommands
{
    public Task<DkpAwardRequestDto> CreateAsync(string discordId, Guid achievementId, string? comment, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var user = await ActiveUserAsync(discordId, ct);
            var achievement = await achievements.FindAsync(achievementId, ct) ?? throw new KeyNotFoundException("Achievement not found.");
            if (!achievement.IsActive) throw new InvalidOperationException("Achievement is inactive.");
            if (await achievements.HasActiveAsync(user.Id, achievementId, ct)) throw new InvalidOperationException("You already have this achievement.");
            if (await requests.HasPendingAsync(user.Id, null, achievementId, ct)) throw new InvalidOperationException("You already have a pending request for this achievement.");
            var normalizedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            if (normalizedComment?.Length > 500) throw new ArgumentException("Comment must be at most 500 characters.");
            var request = new DkpAwardRequest(user.Id, null, achievementId, 1, normalizedComment, time.GetUtcNow().UtcDateTime);
            await requests.AddAsync(request, ct);
            return new DkpAwardRequestDto(request.Id, user.Id, user.DiscordName, null, null, achievement.Id, achievement.Name, achievement.DkpAmount, 1, achievement.Description, normalizedComment, DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, request.CreatedAtUtc, null, null, null, []);
        }, ct);

    public Task<bool> CancelAsync(string discordId, Guid requestId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var user = await ActiveUserAsync(discordId, ct);
            var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
            if (request.UserId != user.Id) throw new UnauthorizedAccessException("You can only cancel your own request.");
            request.Cancel(time.GetUtcNow().UtcDateTime);
            return true;
        }, ct);

    private async Task<User> ActiveUserAsync(string discordId, CancellationToken ct)
    {
        var user = string.IsNullOrWhiteSpace(discordId) ? null : await users.FindByDiscordIdAsync(discordId, ct);
        return user is null || user.IsBlocked ? throw new UnauthorizedAccessException("An active member is required.") : user;
    }
}
