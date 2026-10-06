using DKP.Domain;
using DKP.Facade.Commands;
using Xunit;
namespace DKP.UnitTest;

public sealed class ArchitectureAndPayloadTests
{
    [Fact]
    public void Facade_is_contract_only_and_blazor_has_no_implementation_dependency()
    {
        var facade = typeof(IDkpTransactionCommands).Assembly;
        Assert.DoesNotContain(facade.GetReferencedAssemblies(), a => a.Name!.StartsWith("DKP.Domain") || a.Name.Contains("EntityFrameworkCore"));
        Assert.DoesNotContain(typeof(DKP.Blazor.Program).Assembly.GetReferencedAssemblies(),
            a => a.Name is "DKP.Domain" or "DKP.Application" or "DKP.Infrastructure" || a.Name!.Contains("EntityFrameworkCore"));
        Assert.DoesNotContain(typeof(DKP.Application.Authentication.CommandContext).Assembly.GetReferencedAssemblies(), a => a.Name!.Contains("EntityFrameworkCore"));
        foreach (var method in facade.GetTypes().Where(t => t.IsInterface).SelectMany(t => t.GetMethods()))
            Assert.DoesNotContain(method.GetParameters(), p => p.Name is "actorDiscordId" or "officerDiscordId" or "authenticatedDiscordId");
    }

    [Theory]
    [InlineData("Unknown", "{}", 1)]
    [InlineData("DkpPosted", "{}", 1)]
    [InlineData("DkpPosted", "{\"Amount\":1,\"Reason\":\"ok\"}", 2)]
    [InlineData("PurchasePlaced", "{\"Quantity\":1}", 1)]
    public void Unknown_versions_types_or_missing_payload_fields_are_rejected(string type, string json, int version)
    {
        var user = Guid.NewGuid();
        var entry = new DkpEvent("UserLedger", user, 1, type, user, Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), json, version);
        Assert.ThrowsAny<Exception>(() => LedgerEvents.Read(entry));
    }

    [Fact]
    public void Event_id_is_distinct_from_operation_id_and_round_trips()
    {
        var user = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var payload = new DkpPosted(10, "Reason");
        var entry = LedgerEvents.Create(user, Guid.NewGuid(), 1, operation, DateTime.UtcNow, payload);
        Assert.NotEqual(operation, entry.Id);
        Assert.Equal(operation, entry.CorrelationId);
        Assert.Equal(payload, LedgerEvents.Read(entry));
    }
}
