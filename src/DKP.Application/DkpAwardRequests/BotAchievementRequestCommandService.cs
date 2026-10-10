using DKP.Application.Persistence;
using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;

namespace DKP.Application.DkpAwardRequests;

public sealed class BotAchievementRequestCommandService(
    IUserRepository users,
    IDkpAwardRequestRepository requests,
    IAchievementRepository achievements,
    INotificationOutboxRepository outbox,
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
            await outbox.AddAsync(RequestNotificationFactory.Create(request, user.DiscordName, achievement.Name, achievement.DkpAmount, "Achievement", request.CreatedAtUtc), ct);
            return new DkpAwardRequestDto(request.Id, user.Id, user.DiscordName, null, null, achievement.Id, achievement.Name, achievement.DkpAmount, 1, achievement.Description, normalizedComment, DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, request.CreatedAtUtc, null, null, null, []);
        }, ct);

    public Task<bool> CancelAsync(string discordId, Guid requestId, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync([], async () =>
        {
            var user = await ActiveUserAsync(discordId, ct);
            var request = await requests.FindAsync(requestId, ct) ?? throw new KeyNotFoundException("DKP request not found.");
            if (request.UserId != user.Id) throw new UnauthorizedAccessException("You can only cancel your own request.");
            var now = time.GetUtcNow().UtcDateTime;
            request.Cancel(now);
            await outbox.RequestDeletionAsync(request.Id, now, ct);
            return true;
        }, ct);

    public Task<IReadOnlyList<DkpAwardRequestDto>> CreateForUsersAsync(string discordId, BotMultiAchievementRequest input, CancellationToken ct = default)
        => unitOfWork.ExecuteAsync<IReadOnlyList<DkpAwardRequestDto>>([], async () =>
        {
            await ActiveUserAsync(discordId, ct);
            var achievement = await achievements.FindAsync(input.AchievementId, ct) ?? throw new KeyNotFoundException("Achievement not found.");
            if (!achievement.IsActive) throw new InvalidOperationException("Achievement is inactive.");
            var comment = NormalizeComment(input.Comment);
            var ids = new[] { discordId }.Concat(input.TargetDiscordIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
            if (ids.Length == 0) throw new ArgumentException("At least one member is required.");

            var usersByDiscordId = new Dictionary<string, User>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                var user = await ActiveUserAsync(id, ct);
                usersByDiscordId[id] = user;
            }

            var targets = usersByDiscordId.Values.ToArray();
            foreach (var target in targets)
            {
                if (await achievements.HasActiveAsync(target.Id, achievement.Id, ct))
                    throw new InvalidOperationException($"{target.DiscordName} already has this achievement.");
                if (await requests.HasPendingAsync(target.Id, null, achievement.Id, ct))
                    throw new InvalidOperationException($"{target.DiscordName} already has a pending request for this achievement.");
            }

            var now = time.GetUtcNow().UtcDateTime;
            var result = new List<DkpAwardRequestDto>(targets.Length);
            foreach (var target in targets)
            {
                var created = new DkpAwardRequest(target.Id, null, achievement.Id, 1, comment, now);
                await requests.AddAsync(created, ct);
                await outbox.AddAsync(RequestNotificationFactory.Create(created, target.DiscordName, achievement.Name, achievement.DkpAmount, "Achievement", now), ct);
                result.Add(new DkpAwardRequestDto(created.Id, target.Id, target.DiscordName, null, null, achievement.Id, achievement.Name, achievement.DkpAmount, 1, achievement.Description, comment, DKP.Facade.Contracts.DkpAwardRequestStatus.Pending, now, null, null, null, []));
            }
            return result;
        }, ct);

    private async Task<User> ActiveUserAsync(string discordId, CancellationToken ct)
    {
        var user = string.IsNullOrWhiteSpace(discordId) ? null : await users.FindByDiscordIdAsync(discordId, ct);
        return user is null || user.IsBlocked ? throw new UnauthorizedAccessException("An active member is required.") : user;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment)) return null;
        var normalized = comment.Trim();
        if (normalized.Length > 500) throw new ArgumentException("Comment must be at most 500 characters.");
        return normalized;
    }
}
