using System.Net.Http.Json;
using System.Text.Json;

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

    public Task<IReadOnlyList<BotDkpSourceDto>?> GetDkpSourcesAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotDkpSourceDto>>(HttpMethod.Get, "/api/bot/dkp/sources", discordUserId, null, cancellationToken);

    public Task<IReadOnlyList<BotAwardRequestDto>?> RequestDkpAsync(string discordUserId, Guid presetId, int quantity, IReadOnlyList<string>? targetDiscordIds, string? comment, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotAwardRequestDto>>(HttpMethod.Post, targetDiscordIds is { Count: > 0 } ? "/api/bot/dkp/requests/multi" : "/api/bot/dkp/requests", discordUserId, new BotDkpRequestInput(presetId, quantity, targetDiscordIds, comment), cancellationToken);

    public async Task<bool> CancelDkpRequestAsync(string discordUserId, Guid requestId, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Delete, $"/api/bot/dkp/requests/{requestId:D}", discordUserId, null, cancellationToken) is not null;

    public Task<IReadOnlyList<BotAwardRequestDto>?> GetPendingAwardRequestsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotAwardRequestDto>>(HttpMethod.Get, "/api/bot/requests/pending", discordUserId, null, cancellationToken);

    public async Task<bool> ApproveAwardRequestAsync(string discordUserId, Guid requestId, string? comment, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Post, $"/api/bot/requests/{requestId:D}/approve", discordUserId, new ReviewInput(comment), cancellationToken) is not null;

    public async Task<bool> RejectAwardRequestAsync(string discordUserId, Guid requestId, string? comment, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Post, $"/api/bot/requests/{requestId:D}/reject", discordUserId, new ReviewInput(comment), cancellationToken) is not null;

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

    public Task<IReadOnlyList<BotShopItemDto>?> GetShopItemsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotShopItemDto>>(HttpMethod.Get, "/api/bot/shop", discordUserId, null, cancellationToken);

    public Task<IReadOnlyList<BotBulletinItemDto>?> GetBulletinItemsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotBulletinItemDto>>(HttpMethod.Get, "/api/bot/bulletin", discordUserId, null, cancellationToken);

    public Task<BulletinClaimDto?> ClaimBulletinAsync(string discordUserId, Guid itemId, int quantity, CancellationToken cancellationToken)
        => SendShopAsync<BulletinClaimDto>(HttpMethod.Post, "/api/bot/bulletin/claims", discordUserId, new BotBulletinClaimInput(itemId, quantity), cancellationToken);

    public Task<IReadOnlyList<BulletinClaimDto>?> GetPendingBulletinClaimsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BulletinClaimDto>>(HttpMethod.Get, "/api/bot/bulletin/claims/pending", discordUserId, null, cancellationToken);

    public async Task<bool> ApproveBulletinClaimAsync(string discordUserId, Guid claimId, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Post, $"/api/bot/bulletin/claims/{claimId:D}/approve", discordUserId, null, cancellationToken) is not null;

    public async Task<bool> DenyBulletinClaimAsync(string discordUserId, Guid claimId, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Post, $"/api/bot/bulletin/claims/{claimId:D}/deny", discordUserId, null, cancellationToken) is not null;

    public Task<IReadOnlyList<BotShopAvailabilityDto>?> GetShopAvailabilityAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotShopAvailabilityDto>>(HttpMethod.Get, "/api/bot/shop/availability", discordUserId, null, cancellationToken);

    public Task<IReadOnlyList<BotShopPurchaseDto>?> GetPurchasesAsync(string discordUserId, string status, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotShopPurchaseDto>>(HttpMethod.Get, $"/api/bot/shop/purchases?status={Uri.EscapeDataString(status)}", discordUserId, null, cancellationToken);

    public Task<BotShopPurchaseDto?> PurchaseAsync(string discordUserId, ShopPurchaseInput input, CancellationToken cancellationToken)
        => SendShopAsync<BotShopPurchaseDto>(HttpMethod.Post, "/api/bot/shop/purchases", discordUserId, input, cancellationToken);

    public async Task<bool> CancelPurchaseAsync(string discordUserId, Guid purchaseId, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Delete, $"/api/bot/shop/purchases/{purchaseId:D}", discordUserId, null, cancellationToken) is not null;

    public Task<BotAchievementOverviewDto?> GetAchievementsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<BotAchievementOverviewDto>(HttpMethod.Get, "/api/bot/achievements", discordUserId, null, cancellationToken);

    public Task<BotAwardRequestDto?> RequestAchievementAsync(string discordUserId, Guid achievementId, string? comment, CancellationToken cancellationToken)
        => SendShopAsync<BotAwardRequestDto>(HttpMethod.Post, "/api/bot/achievements/requests", discordUserId, new AchievementRequestInput(achievementId, comment), cancellationToken);

    public async Task<bool> CancelAchievementRequestAsync(string discordUserId, Guid requestId, CancellationToken cancellationToken)
        => await SendShopAsync<object>(HttpMethod.Delete, $"/api/bot/achievements/requests/{requestId:D}", discordUserId, null, cancellationToken) is not null;

    public Task<BotAccountDto?> CreateAccountAsync(string discordUserId, BotAccountInput input, CancellationToken cancellationToken)
        => SendShopAsync<BotAccountDto>(HttpMethod.Post, "/api/bot/account", discordUserId, input, cancellationToken);

    public Task<BotReviewNotificationPreferenceDto?> ToggleReviewNotificationsAsync(string discordUserId, CancellationToken cancellationToken)
        => SendShopAsync<BotReviewNotificationPreferenceDto>(HttpMethod.Post, "/api/bot/notifications/preferences/review-toggle", discordUserId, null, cancellationToken);

    public Task<IReadOnlyList<BotNotificationDto>?> GetPendingNotificationsAsync(CancellationToken cancellationToken, string? target = null)
        => SendBotAsync<IReadOnlyList<BotNotificationDto>>(HttpMethod.Get, $"/api/bot/notifications/pending?limit=10{(target is null ? string.Empty : $"&target={Uri.EscapeDataString(target)}")}", null, cancellationToken);

    public Task<IReadOnlyList<BotForumSectionDto>?> GetForumSectionsAsync(CancellationToken cancellationToken)
        => SendBotAsync<IReadOnlyList<BotForumSectionDto>>(HttpMethod.Get, "/api/bot/forum-sections", null, cancellationToken);

    public Task<BotForumSectionDto?> RegisterForumSectionAsync(string sectionKey, ulong forumChannelId, ulong threadId, ulong initialMessageId, CancellationToken cancellationToken)
        => SendBotAsync<BotForumSectionDto>(HttpMethod.Post, "/api/bot/forum-sections/register",
            new RegisterForumSectionInput(settings.GuildId.ToString(), sectionKey, forumChannelId, threadId, initialMessageId), cancellationToken);

    public async Task<bool> MarkNotificationSentAsync(Guid id, ulong messageId, CancellationToken cancellationToken)
        => await SendBotAsync<object>(HttpMethod.Post, $"/api/bot/notifications/{id:D}/sent", new NotificationSentInput(messageId), cancellationToken) is not null;

    public async Task<bool> MarkNotificationDeletedAsync(Guid id, CancellationToken cancellationToken)
        => await SendBotAsync<object>(HttpMethod.Post, $"/api/bot/notifications/{id:D}/deleted", null, cancellationToken) is not null;

    public async Task<bool> MarkNotificationFailedAsync(Guid id, string error, CancellationToken cancellationToken)
        => await SendBotAsync<object>(HttpMethod.Post, $"/api/bot/notifications/{id:D}/failed", new NotificationFailureInput(error), cancellationToken) is not null;

    public Task<IReadOnlyList<BotAwardRequestDto>?> RequestAchievementForUsersAsync(string discordUserId, Guid achievementId, IReadOnlyList<string> targetDiscordIds, string? comment, CancellationToken cancellationToken)
        => SendShopAsync<IReadOnlyList<BotAwardRequestDto>>(HttpMethod.Post, "/api/bot/achievements/requests/multi", discordUserId, new MultiAchievementRequestInput(achievementId, targetDiscordIds, comment), cancellationToken);

    private async Task<T?> SendShopAsync<T>(HttpMethod method, string path, string discordUserId, object? body, CancellationToken cancellationToken)
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
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("DKP shop API request failed with status {StatusCode}: {Error}", response.StatusCode, error.Length > 500 ? error[..500] : error);
                return default;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return (T)(object)true;
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "DKP shop API request failed.");
            return default;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("DKP shop API request timed out.");
            return default;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "DKP shop API returned an unexpected response format.");
            return default;
        }
    }

    private async Task<T?> SendBotAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(settings.ApiBaseUrl, path));
        request.Headers.Add("X-DKP-Bot-Secret", settings.ApiSecret);
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        if (body is not null) request.Content = JsonContent.Create(body);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) { logger.LogWarning("DKP notification API request failed with status {StatusCode}", response.StatusCode); return default; }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return (T)(object)true;
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "DKP notification API request failed.");
            return default;
        }
    }

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
public sealed record BotDkpSourceDto(Guid PresetId, string Name, int Amount, string Reason, int Applications, int? MaxApplications, int? Remaining);
public sealed record BotDkpRequestInput(Guid PresetId, int Quantity, IReadOnlyList<string>? TargetDiscordIds, string? Comment);
public sealed record ReviewInput(string? Comment);
public sealed record CharacterInputDto(string FirstName, string LastName);
public sealed record BotCharacterDto(Guid Id, string FirstName, string LastName, bool IsMain);
public sealed record ShopPurchaseInput(Guid ShopItemId, int Quantity);
public sealed record BotShopItemDto(Guid Id, string Key, string Name, string Description, int Price, int MaxPerUser, bool IsActive, IReadOnlyList<BotShopRequirementDto>? AchievementRequirements);
public sealed record BotBulletinItemDto(Guid Id, string Title, string Description, int DkpAmount, int RemainingQuantity, bool UniquePerUser, DateTime? DeadlineUtc);
public sealed record BotBulletinClaimInput(Guid BulletinItemId, int Quantity);
public sealed record BulletinClaimDto(Guid Id, Guid BulletinItemId, Guid UserId, string DiscordName, string? MainCharacter, string BulletinTitle, int Quantity, string? Comment, JsonElement Status, DateTime CreatedAtUtc);
public sealed record BotShopAvailabilityDto(Guid ShopItemId, string Key, string Name, string Description, int Price, int MaxPerUser, int? RollBonusValue, int ActiveQuantity, int UsedQuantity, int CountedQuantity, int RemainingQuantity, bool IsAvailable, IReadOnlyList<BotShopRequirementDto> AchievementRequirements, string? PurchaseLimitKey = null, int? MaxActivePerUser = null, int? MaxPerDay = null, int? MaxPerWeek = null, int? MaxPerMonth = null, int? DailyRemaining = null, int? WeeklyRemaining = null, int? MonthlyRemaining = null)
{
    public bool IsRollBonus => RollBonusValue is not null;
}
public sealed record BotShopRequirementDto(Guid AchievementId, string AchievementName);
public sealed record BotShopPurchaseDto(Guid Id, Guid UserId, string UserName, string? MainCharacterName, Guid ShopItemId, string ItemName, int Quantity, int TotalDkpCost, DateTime CreatedAtUtc, DateTime? CancelledAtUtc, bool IsUsed = false, bool IsManuallyUsed = false)
{
    public bool IsCancelled => CancelledAtUtc is not null;
    public string Status => IsCancelled ? "cancelled" : IsUsed ? "used" : "active";
}
public sealed record AchievementRequestInput(Guid AchievementId, string? Comment);
public sealed record MultiAchievementRequestInput(Guid AchievementId, IReadOnlyList<string> TargetDiscordIds, string? Comment);
public sealed record BotAccountInput(string DiscordName, string? AvatarUrl);
public sealed record BotAccountDto(Guid Id, string DiscordId, string DiscordName, int Role, bool Created);
public sealed record BotReviewNotificationPreferenceDto(bool Enabled);
public sealed record BotNotificationDto(Guid Id, string NotificationType, string Payload, int Attempts, string Action, ulong? DiscordMessageId, string? RecipientDiscordUserId);
public sealed record BotForumSectionDto(Guid Id, string GuildId, string SectionKey, ulong? ForumChannelId, ulong? ThreadId, ulong? InitialMessageId, string InitialMessageContent, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record RegisterForumSectionInput(string GuildId, string SectionKey, ulong ForumChannelId, ulong ThreadId, ulong InitialMessageId);
public sealed record NotificationFailureInput(string? Error);
public sealed record NotificationSentInput(ulong MessageId);
public sealed record BotAchievementOverviewDto(IReadOnlyList<BotAchievementDefinitionDto> Definitions, IReadOnlyList<BotUserAchievementDto> UserAchievements, IReadOnlyList<BotAwardRequestDto> Requests);
public sealed record BotAchievementDefinitionDto(Guid Id, string Key, string Name, string Description, int DkpAmount, bool IsActive);
public sealed record BotUserAchievementDto(Guid Id, Guid UserId, string DiscordName, Guid AchievementId, string AchievementName, int DkpAmount, bool IsActive, DateTime GrantedAtUtc, DateTime? RevokedAtUtc);
public sealed record BotAwardRequestDto(Guid Id, Guid UserId, string DiscordName, string? MainCharacter, Guid? PresetId, Guid? AchievementId, string PresetName, int Amount, int Quantity, string Reason, string? Comment, int Status, DateTime CreatedAtUtc, DateTime? ReviewedAtUtc, string? ReviewedByDiscordName, string? ReviewComment, IReadOnlyList<Guid> DkpEventIds)
{
    public bool IsPending => Status == 0;
    public string StatusName => Status switch { 0 => "Pending", 1 => "Approved", 2 => "Rejected", 3 => "Cancelled", _ => "Unknown" };
}
