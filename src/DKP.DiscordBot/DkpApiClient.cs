using System.Net.Http.Json;

namespace DKP.DiscordBot;

public sealed class DkpApiClient(HttpClient httpClient, DiscordBotSettings settings, ILogger<DkpApiClient> logger)
{
    public async Task<BotHealthResponse?> GetHealthAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(settings.ApiBaseUrl, "/api/bot/health"));
        request.Headers.Add("X-DKP-Bot-Secret", settings.ApiSecret);
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DKP API health request failed with status {StatusCode}", response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<BotHealthResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "DKP API health request failed.");
            return null;
        }
    }
}

public sealed record BotHealthResponse(string Status, string Service, string CorrelationId);
