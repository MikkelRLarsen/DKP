using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DKP.Blazor;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using DKP.Facade.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DKP.UnitTest;

public sealed class HostSmokeTests : DatabaseTest
{
    [Fact]
    public async Task Real_host_renders_member_and_officer_pages_with_fake_authentication()
    {
        await FundAsync(Member.Id, 123);
        await As("member").Characters.CreateAsync(new("Smoke", "Player"));
        await using var host = new SmokeHost(ConnectionString);
        using var member = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        member.DefaultRequestHeaders.Add("X-Test-User", "member");
        foreach (var path in new[] { "/", "/members", $"/members/{Member.Id}", "/my-dkp", "/my-dkp/sources", "/dkp-shop", "/my-purchases", "/activity" })
        {
            var response = await member.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {html[..Math.Min(html.Length, 400)]}");
            Assert.DoesNotContain("Unable to load", html);
            Assert.DoesNotContain("An unhandled exception", html);
            if (path is "/" or "/my-dkp") Assert.Contains("123", html);
        }
        foreach (var path in new[] { "/admin/dkp", "/admin/users", "/admin/shop", "/admin/shop/purchases", "/admin/dkp-presets", "/admin/dkp-requests", "/admin/loot-reserve" })
        {
            var response = await member.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        using var officer = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        officer.DefaultRequestHeaders.Add("X-Test-User", "officer");
        foreach (var path in new[] { "/admin/dkp", "/admin/users", "/admin/shop", "/admin/shop/purchases", "/admin/dkp-presets", "/admin/dkp-requests", "/admin/loot-reserve" })
        {
            var response = await officer.GetAsync(path);
            var html = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode}");
            Assert.DoesNotContain("Unable to load", html);
        }
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/activity")).StatusCode);
    }

    [Fact]
    public async Task Bot_health_requires_the_configured_service_secret()
    {
        await using var host = new SmokeHost(ConnectionString);
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bot/health")).StatusCode);

        client.DefaultRequestHeaders.Add("X-DKP-Bot-Secret", "wrong-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bot/health")).StatusCode);

        client.DefaultRequestHeaders.Remove("X-DKP-Bot-Secret");
        client.DefaultRequestHeaders.Add("X-DKP-Bot-Secret", "test-bot-secret");
        var response = await client.GetAsync("/api/bot/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("dkp-api", body);
        Assert.Contains("ok", body);
    }

    [Fact]
    public async Task Dependency_injection_resolves_every_facade_contract_and_uses_one_constructor()
    {
        await using var host = new SmokeHost(ConnectionString);
        using var scope = host.Services.CreateScope();
        var interfaces = typeof(IDkpTransactionCommands).Assembly.GetTypes().Where(t => t.IsInterface);
        foreach (var type in interfaces)
            Assert.NotNull(scope.ServiceProvider.GetRequiredService(type));
        Assert.Single(typeof(DKP.Application.DkpTransactions.DkpTransactionCommandService).GetConstructors());
    }

    [Fact]
    public async Task Cookie_validation_rejects_an_existing_session_after_blocking()
    {
        await using var host = new SmokeHost(ConnectionString);
        using var scope = host.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>().Get("Cookies");
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "member")], "Cookies"));
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal };
        var scheme = new AuthenticationScheme("Cookies", null, typeof(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler));
        var valid = new Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext(http, scheme, options, new(principal, "Cookies"));
        await options.Events.ValidatePrincipal(valid);
        Assert.NotNull(valid.Principal);
        await As("officer").Blocks.BlockAsync(new(Member.Id, "Blocked after login"));
        var blocked = new Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext(http, scheme, options, new(principal, "Cookies"));
        await options.Events.ValidatePrincipal(blocked);
        Assert.Null(blocked.Principal);
    }

    private sealed class SmokeHost(string connection) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Discord:ClientId"] = "test-client",
                ["Discord:ClientSecret"] = "test-secret-not-a-credential",
                ["Discord:GuildId"] = "1505886353131311136",
                ["DkpBot:ApiSecret"] = "test-bot-secret"
            }));
            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            });
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        }
    }

    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var id = Request.Headers["X-Test-User"].ToString();
            if (string.IsNullOrEmpty(id)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, id),
                new Claim(ClaimTypes.Role, id == "officer" ? "Officer" : "Member")], "Test"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
        }
    }
}
