using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.DkpAwardRequests;

internal static class RequestNotificationFactory
{
    public static DiscordNotificationOutbox Create(DkpAwardRequest request, string userName, string sourceName, int amount, string kind, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new RequestNotificationPayload(kind, userName, sourceName, amount, request.Quantity, request.Comment, now));
        return new DiscordNotificationOutbox("DkpAwardRequestCreated", payload, now, request.Id);
    }

    private sealed record RequestNotificationPayload(string Kind, string UserName, string SourceName, int Amount, int Quantity, string? Comment, DateTime CreatedAtUtc);
}
