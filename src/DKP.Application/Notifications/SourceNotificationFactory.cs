using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.Notifications;

internal static class SourceNotificationFactory
{
    public static DiscordNotificationOutbox Create(string kind, string name, string description, int amount, int? limit, string createdBy, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new SourceCreatedPayload(kind, name, description, amount, limit, createdBy, now));
        return new DiscordNotificationOutbox("DkpSourceCreated", payload, now);
    }

    private sealed record SourceCreatedPayload(string Kind, string Name, string Description, int Amount, int? Limit, string CreatedBy, DateTime CreatedAtUtc);
}
