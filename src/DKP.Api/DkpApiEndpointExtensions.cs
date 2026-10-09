using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DKP.Api;

public interface IBotServiceAuthenticator
{
    bool IsAuthenticated(HttpRequest request);
}

public sealed record BotHealthResponse(string Status, string Service, string CorrelationId);

public static class DkpApiEndpointExtensions
{
    public static IServiceCollection AddDkpApi(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["DkpBot:ApiSecret"] ?? configuration["DKP_BOT_API_SECRET"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Missing required bot API configuration: DkpBot:ApiSecret (DKP_BOT_API_SECRET).");

        services.AddControllers()
            .AddApplicationPart(typeof(BotHealthController).Assembly);
        services.AddSingleton<IBotServiceAuthenticator>(new BotServiceAuthenticator(secret));
        return services;
    }

    private sealed class BotServiceAuthenticator(string expectedSecret) : IBotServiceAuthenticator
    {
        public bool IsAuthenticated(HttpRequest request)
        {
            if (string.IsNullOrEmpty(expectedSecret)) return false;
            var suppliedSecret = request.Headers["X-DKP-Bot-Secret"].FirstOrDefault();
            if (string.IsNullOrEmpty(suppliedSecret)) return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSecret), Encoding.UTF8.GetBytes(suppliedSecret));
        }
    }
}
