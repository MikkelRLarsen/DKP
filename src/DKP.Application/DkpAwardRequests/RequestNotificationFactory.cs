using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.DkpAwardRequests;

internal static class RequestNotificationFactory
{
    public static DiscordNotificationOutbox Create(DkpAwardRequest request, string userName, string sourceName, int amount, string kind, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new RequestNotificationPayload(request.Id, kind, userName, sourceName, amount, request.Quantity, request.Comment, now));
        return new DiscordNotificationOutbox("DkpAwardRequestCreated", payload, now, request.Id);
    }

    public static DiscordNotificationOutbox CreateReview(DkpAwardRequest request, string recipientDiscordId, string userName, string sourceName, int amount, string kind, bool approved, int? newBalance, string? reviewComment, string reviewerName, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new RequestReviewNotificationPayload(
            kind, userName, sourceName, amount, request.Quantity, approved ? "approved" : "rejected", newBalance, reviewComment, reviewerName, now));
        return new DiscordNotificationOutbox("DkpAwardRequestReviewed", payload, now, request.Id, recipientDiscordId);
    }

    private sealed record RequestNotificationPayload(Guid RequestId, string Kind, string UserName, string SourceName, int Amount, int Quantity, string? Comment, DateTime CreatedAtUtc);
    private sealed record RequestReviewNotificationPayload(string Kind, string UserName, string SourceName, int Amount, int Quantity, string Action, int? NewBalance, string? ReviewComment, string ReviewerName, DateTime ReviewedAtUtc);
}
