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
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("DKP API health request timed out.");
            return null;
        }
    }

    public Task<DkpHistoryDto?> GetDkpAsync(string discordUserId, CancellationToken cancellationToken)
        => GetDkpAsync("/api/bot/dkp", discordUserId, cancellationToken);

    public Task<DkpHistoryDto?> GetDkpHistoryAsync(string discordUserId, int limit, CancellationToken cancellationToken)
        => GetDkpAsync($"/api/bot/dkp/history?limit={limit}", discordUserId, cancellationToken);

    public async Task<IReadOnlyList<BotCharacterDto>?> GetCharactersAsync(string discordUserId, CancellationToken cancellationToken)
        => await SendCharactersAsync(HttpMethod.Get, "/api/bot/characters", discordUserId, null, cancellationToken) as IReadOnlyList<BotCharacterDto>;

    public async Task<BotCharacterDto?> CreateCharacterAsync(string discordUserId, CharacterInputDto input, CancellationToken cancellationToken)
        => await SendCharactersAsync(HttpMethod.Post, "/api/bot/characters", discordUserId, input, cancellationToken) as BotCharacterDto;

    public async Task<BotCharacterDto?> UpdateCharacterAsync(string discordUserId, Guid characterId, CharacterInputDto input, CancellationToken cancellationToken)
        => await SendCharactersAsync(HttpMethod.Put, $"/api/bot/characters/{characterId:D}", discordUserId, input, cancellationToken) as BotCharacterDto;

    public async Task<bool> DeleteCharacterAsync(string discordUserId, Guid characterId, CancellationToken cancellationToken)
        => await SendCharactersAsync(HttpMethod.Delete, $"/api/bot/characters/{characterId:D}", discordUserId, null, cancellationToken) is not null;

    public async Task<bool> SetMainCharacterAsync(string discordUserId, Guid characterId, CancellationToken cancellationToken)
        => await SendCharactersAsync(HttpMethod.Put, $"/api/bot/characters/{characterId:D}/main", discordUserId, null, cancellationToken) is not null;

    private async Task<object?> SendCharactersAsync(HttpMethod method, string path, string discordUserId, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(settings.ApiBaseUrl, path));
        request.Headers.Add("X-DKP-Bot-Secret", settings.ApiSecret);
        request.Headers.Add("X-DKP-Discord-User-Id", discordUserId);
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        if (body is not null) request.Content = JsonContent.Create(body);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DKP character API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return true;
            return method == HttpMethod.Get
                ? await response.Content.ReadFromJsonAsync<IReadOnlyList<BotCharacterDto>>(cancellationToken: cancellationToken)
                : await response.Content.ReadFromJsonAsync<BotCharacterDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "DKP character API request failed.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("DKP character API request timed out.");
            return null;
        }
    }

    private async Task<DkpHistoryDto?> GetDkpAsync(string path, string discordUserId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(settings.ApiBaseUrl, path));
        request.Headers.Add("X-DKP-Bot-Secret", settings.ApiSecret);
        request.Headers.Add("X-DKP-Discord-User-Id", discordUserId);
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DKP API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<DkpHistoryDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "DKP API request failed.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("DKP API request timed out.");
            return null;
        }
    }
}

public sealed record BotHealthResponse(string Status, string Service, string CorrelationId);
public sealed record DkpHistoryDto(BalanceDto Balance, IReadOnlyList<DkpTransactionDto> Transactions);
public sealed record BalanceDto(int Amount);
public sealed record DkpTransactionDto(Guid Id, int Amount, string Reason, DateTime CreatedAtUtc, string CreatedByDiscordName);
public sealed record CharacterInputDto(string FirstName, string LastName);
public sealed record BotCharacterDto(Guid Id, string FirstName, string LastName, bool IsMain);
