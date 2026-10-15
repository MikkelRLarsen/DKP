using System.Text.Json;
using DKP.Domain;

namespace DKP.Application.Notifications;

internal static class SourceNotificationFactory
{
    public static DiscordNotificationOutbox Create(string kind, Guid sourceId, string name, string description, int amount, int? limit, string createdBy, DateTime now)
    {
        var payload = JsonSerializer.Serialize(new SourceCreatedPayload(kind, sourceId, name, description, amount, limit, createdBy, now));
        return new DiscordNotificationOutbox("DkpSourceCreated", payload, now, sourceId);
    }

    private sealed record SourceCreatedPayload(string Kind, Guid SourceId, string Name, string Description, int Amount, int? Limit, string CreatedBy, DateTime CreatedAtUtc);
}
