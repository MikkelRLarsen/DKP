using System.Reflection;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DKP.DiscordBot;

public sealed class DiscordBotWorker(DiscordBotSettings settings, DkpApiClient api, ILogger<DiscordBotWorker> logger) : IHostedService, IDisposable
{
    private readonly SemaphoreSlim registrationLock = new(1, 1);
    private readonly SemaphoreSlim forumSyncLock = new(1, 1);
    private DiscordSocketClient? client;
    private InteractionService? interactions;
    private ServiceProvider? services;
    private ulong? bulletinCommandId;
    private ulong? dkpCommandId;
    private CancellationTokenSource? notificationCts;
    private Task? notificationTask;
    private Task? forumSyncTask;
    private readonly Dictionary<string, ulong> forumThreads = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ulong> forumMessages = new(StringComparer.OrdinalIgnoreCase);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = false
        });
        interactions = new InteractionService(client.Rest, new InteractionServiceConfig { EnableAutocompleteHandlers = true });
        services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(api)
            .AddSingleton(settings)
            .AddSingleton(interactions)
            .BuildServiceProvider();
        client.Log += LogAsync;
        client.Ready += RegisterCommandsAsync;
        client.Disconnected += exception => { logger.LogWarning(exception, "Discord Gateway disconnected; the client will reconnect."); return Task.CompletedTask; };
        client.InteractionCreated += HandleInteractionAsync;
        await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
        await client.LoginAsync(TokenType.Bot, settings.BotToken);
        await client.StartAsync();
        notificationCts = new CancellationTokenSource();
        notificationTask = NotificationLoopAsync(notificationCts.Token);
        forumSyncTask = ForumSyncLoopAsync(notificationCts.Token);
        logger.LogInformation("Discord notification delivery enabled. Officer channel: {OfficerChannelId}; member mode: {MemberNotificationMode}; member channel: {MemberChannelId}.", settings.NotificationChannelId, settings.MemberNotificationMode, settings.MemberNotificationChannelId);
        logger.LogInformation("Discord bot startup initiated for guild {GuildId}.", settings.GuildId);
    }

    private async Task NotificationLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pending = await api.GetPendingNotificationsAsync(cancellationToken, settings.NotificationChannelId is null && settings.MemberNotificationChannelId is null ? "dm" : null);
                if (pending is not null)
                {
                    foreach (var notification in pending)
                    {
                        try
                        {
                            if (notification.Action == "delete")
                            {
                                var channel = await GetNotificationChannelAsync(notification.NotificationType, notification.Payload);
                                if (channel is null)
                                    throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                if (notification.DiscordMessageId is ulong messageId)
                                {
                                    var message = await channel.GetMessageAsync(messageId);
                                    if (message is not null) await message.DeleteAsync();
                                }
                                await api.MarkNotificationDeletedAsync(notification.Id, cancellationToken);
                            }
                            else
                            {
                                IMessage message;
                                if (ulong.TryParse(notification.RecipientDiscordUserId, out var recipientId))
                                {
                                    var recipient = await client!.Rest.GetUserAsync(recipientId);
                                    if (recipient is null)
                                        throw new InvalidOperationException("The Discord recipient could not be resolved.");
                                    var dm = await recipient.CreateDMChannelAsync();
                                    message = await dm.SendMessageAsync(FormatPrivateNotification(notification.Payload), allowedMentions: AllowedMentions.None);
                                }
                                else
                                {
                                    var channel = await GetNotificationChannelAsync(notification.NotificationType, notification.Payload);
                                    if (channel is null)
                                        throw new InvalidOperationException("The configured Discord notification channel is unavailable.");
                                    var content = FormatNotification(notification.NotificationType, notification.Payload);
                                    var components = notification.NotificationType switch
                                    {
                                        "BulletinItem" => BuildBulletinComponents(notification.Payload),
                                        "BulletinClaimCreated" => BuildBulletinClaimComponents(notification.Payload),
                                        "DkpAwardRequestCreated" => BuildAwardRequestComponents(notification.Payload),
                                        "DkpSourceCreated" => BuildSourceComponents(notification.Payload),
                                        "ShopItem" => BuildShopComponents(notification.Payload),
                                        _ => null
                                    };
                                    if (notification.Action == "update" && notification.DiscordMessageId is ulong messageId)
                                    {
                                        var existing = await channel.GetMessageAsync(messageId);
                                        if (existing is IUserMessage existingUserMessage)
                                        {
                                            await existingUserMessage.ModifyAsync(properties =>
                                            {
                                                properties.Content = content;
                                                properties.Components = components;
                                            });
                                            message = existingUserMessage;
                                        }
                                        else
                                        {
                                            message = await channel.SendMessageAsync(content, components: components, allowedMentions: AllowedMentions.None);
                                        }
                                    }
                                    else
                                    {
                                        message = await channel.SendMessageAsync(content, components: components, allowedMentions: AllowedMentions.None);
                                    }
                                }
                                await api.MarkNotificationSentAsync(notification.Id, message.Id, cancellationToken);
                            }
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            logger.LogWarning(exception, "Unable to deliver notification {NotificationId}.", notification.Id);
                            await api.MarkNotificationFailedAsync(notification.Id, exception.Message, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Discord notification polling failed.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
        }
    }

    private async Task ForumSyncLoopAsync(CancellationToken cancellationToken)
    {
        if (settings.MemberNotificationMode != "forum" || settings.MemberForumChannelId is null) return;
        while (!cancellationToken.IsCancellationRequested)
        {
            try { await EnsureForumSectionsAsync(cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogWarning(exception, "Discord forum section synchronization failed."); }
            try { await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
        }
    }

    private async Task EnsureForumSectionsAsync(CancellationToken cancellationToken)
    {
        await forumSyncLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureForumSectionsCoreAsync(cancellationToken);
        }
        finally
        {
            forumSyncLock.Release();
        }
    }

    private async Task EnsureForumSectionsCoreAsync(CancellationToken cancellationToken)
    {
        if (client is null || settings.MemberForumChannelId is not ulong forumId) return;
        var channel = client.GetChannel(forumId) as IForumChannel ?? await client.Rest.GetChannelAsync(forumId) as IForumChannel;
        if (channel is not IForumChannel forum)
        {
            logger.LogWarning("Configured member forum channel {ForumChannelId} is not a Discord forum channel.", forumId);
            return;
        }

        var configured = await api.GetForumSectionsAsync(cancellationToken);
        if (configured is null)
        {
            // An unavailable API is not the same as an empty database. Creating
            // posts here would create duplicates on every restart before the API
            // has finished starting.
            logger.LogWarning("Forum section mappings could not be loaded from the DKP API; skipping forum post creation until the next synchronization attempt.");
            return;
        }
        foreach (var key in new[] { "bulletin-board", "achievements", "dkp-sources", "shop", "bot-help" })
        {
            var section = configured.FirstOrDefault(x => x.SectionKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            IThreadChannel? thread = null;
            if (section?.ThreadId is ulong savedThreadId)
            {
                try { thread = await client.Rest.GetChannelAsync(savedThreadId) as IThreadChannel; }
                catch (Exception exception) { logger.LogWarning(exception, "Unable to resolve saved forum section {SectionKey}.", key); }
            }
            else if (section is null && forumThreads.TryGetValue(key, out var processThreadId))
            {
                try { thread = await client.Rest.GetChannelAsync(processThreadId) as IThreadChannel; }
                catch (Exception exception) { logger.LogWarning(exception, "Unable to resolve the forum section {SectionKey} created earlier in this process.", key); }
            }

            ulong messageId;
            if (thread is null)
            {
                thread = await forum.CreatePostAsync(SectionTitle(key), ThreadArchiveDuration.OneWeek,
                    text: section?.InitialMessageContent ?? DefaultForumContent(key), allowedMentions: AllowedMentions.None);
                messageId = thread.Id;
                try
                {
                    if (await thread.GetMessageAsync(thread.Id) is IUserMessage starter) { messageId = starter.Id; await starter.PinAsync(); }
                }
                catch (Exception exception) { logger.LogWarning(exception, "Forum section {SectionKey} was created but could not be pinned.", key); }
                var registered = await api.RegisterForumSectionAsync(key, forum.Id, thread.Id, messageId, cancellationToken);
                if (registered is null)
                    logger.LogWarning("Forum section {SectionKey} was created, but its mapping could not be saved. The bot will retry synchronization without creating another post while this process remains running.", key);
            }
            else
            {
                messageId = section?.InitialMessageId ?? thread.Id;
                try
                {
                    await thread.ModifyAsync(properties => properties.Name = SectionTitle(key));
                    if (await thread.GetMessageAsync(messageId) is IUserMessage message && section is not null && message.Content != section.InitialMessageContent)
                        await message.ModifyAsync(properties => properties.Content = section.InitialMessageContent);
                }
                catch (Exception exception) { logger.LogWarning(exception, "Unable to synchronize initial message for forum section {SectionKey}.", key); }
                if (section is null || section.ForumChannelId != forum.Id || section.ThreadId != thread.Id || section.InitialMessageId != messageId)
                    await api.RegisterForumSectionAsync(key, forum.Id, thread.Id, messageId, cancellationToken);
            }
            forumThreads[key] = thread.Id;
            forumMessages[key] = messageId;
        }
    }

    private static string SectionTitle(string key) => key switch
    {
        "bulletin-board" => "Bulletin Board",
        "achievements" => "Achievements",
        "dkp-sources" => "Repeatable DKP",
        "shop" => "Shop",
        _ => "DKP Bot Help"
    };

    private static string DefaultForumContent(string key) => key switch
    {
        "bulletin-board" => """
            📌 **BULLETIN BOARD**

            This channel contains temporary guild tasks and offers. Each post explains the reward, available quantity and deadline.

            To claim an item, use the button on the post or `/bulletin claim`. Your claim is reviewed by an Officer before the DKP is awarded.

            Website: <https://wowforever.coffeecottage.dk/bulletin>

            **First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.

            Suggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.

            You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.
            """,
        "achievements" => """
            🏆 **ACHIEVEMENTS**

            This channel shows achievements that can be earned for DKP. Read the requirement before requesting one. A request is reviewed by an Officer; it is not granted automatically.

            Use the button on an achievement post or `/achievement request` to submit a request. You can also review your achievements with `/achievements`.

            Website: <https://wowforever.coffeecottage.dk/my-dkp/sources>

            **First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.

            Suggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.

            You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.
            """,
        "dkp-sources" => """
            💰 **REPEATABLE DKP**

            This channel contains repeatable ways to earn DKP, such as approved dungeon or guild activities. Each post shows the amount and any lifetime limit.

            Use the button on a post or `/dkp request` to request DKP. An Officer must approve the request before your balance changes. `/dkp sources` shows your remaining opportunities.

            Website: <https://wowforever.coffeecottage.dk/my-dkp/sources>

            **First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.

            Suggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.

            You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.
            """,
        "shop" => """
            🛒 **DKP SHOP**

            This channel announces available shop items and their prices and purchase limits. Requirements and remaining availability are shown on each post.

            Use the button on an item or `/shop buy` to purchase it. Your personal limits, achievement requirements and DKP balance are checked before the purchase is completed. Use `/purchases` to see your purchases.

            Website: <https://wowforever.coffeecottage.dk/dkp-shop>

            **First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.

            Suggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.

            You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.
            """,
        _ => """
            🤖 **DKP BOT HELP**

            The DKP Bot gives guild members access to common DKP features directly in Discord. You can manage characters, view DKP, request repeatable DKP and achievements, claim Bulletin Board items, buy shop items and view your purchases.

            **Getting started**
            1. Open <https://wowforever.coffeecottage.dk/> and log in with Discord, or run `/account create`.
            2. Create your characters with `/characters create`.
            3. Choose your main with `/characters main`.
            4. Use `/help` whenever you need an overview of the commands.
            5. Use `/notifications toggle` to turn private review notifications on or off.

            Requests and claims are reviewed by Officers. A request does not change your DKP until it is approved. Shop purchases and limits are checked automatically.

            **Questions and suggestions**
            Use this DKP Bot Help post for general questions and suggestions, or contact <@333676904768864268> if you need Officer assistance. Suggestions for new achievements, shop items and repeatable DKP sources are especially welcome. Please keep questions about a specific request in the relevant channel.

            Website: <https://wowforever.coffeecottage.dk/>

            You can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.
            """
    };

    private string FormatNotification(string notificationType, string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (notificationType == "BulletinItem")
            {
                var title = Safe(GetString(root, "Title") ?? "Bulletin item");
                var description = Safe(GetString(root, "Description"));
                var dkp = root.TryGetProperty("DkpAmount", out var dkpElement) ? dkpElement.GetInt32() : 0;
                var remaining = root.TryGetProperty("RemainingQuantity", out var remainingElement) ? remainingElement.GetInt32() : 0;
                var pending = root.TryGetProperty("PendingQuantity", out var pendingElement) ? pendingElement.GetInt32() : 0;
                var max = root.TryGetProperty("MaxCompletions", out var maxElement) ? maxElement.GetInt32() : 0;
                var bulletinMessage = $"📌 **BULLETIN BOARD ITEM**\n\n**{title}**\n{description}\n\nDKP reward: {dkp}\nAvailable quantity: {remaining}/{max}";
                if (pending > 0) bulletinMessage += $"\nPending claims: {pending}";
                if (root.TryGetProperty("DeadlineUtc", out var deadline) && deadline.ValueKind != JsonValueKind.Null && deadline.TryGetDateTime(out var deadlineUtc))
                    bulletinMessage += $"\nDeadline: {deadlineUtc:u}";
                bulletinMessage += bulletinCommandId is ulong commandId
                    ? $"\n\nUse </bulletin claim:{commandId}> or press **Claim Bulletin Item**."
                    : "\n\nUse `/bulletin claim` or press **Claim Bulletin Item**.";
                return bulletinMessage.Trim();
            }
            if (notificationType == "DkpSourceCreated")
            {
                var sourceKind = Safe(GetString(root, "Kind") ?? "DKP source");
                var name = Safe(GetString(root, "Name") ?? "Unknown source");
                var description = Safe(GetString(root, "Description"));
                var sourceAmount = root.TryGetProperty("Amount", out var sourceAmountElement) ? sourceAmountElement.GetInt32() : 0;
                var limit = root.TryGetProperty("Limit", out var limitElement) && limitElement.ValueKind != JsonValueKind.Null ? limitElement.GetInt32().ToString() : null;
                var sourceMessage = $"**New {sourceKind} available**\nName: {name}\nAmount: {sourceAmount} DKP";
                if (!string.IsNullOrWhiteSpace(limit)) sourceMessage += $"\nLimit: {limit} per user";
                var createdBy = Safe(GetString(root, "CreatedBy"));
                if (!string.IsNullOrWhiteSpace(createdBy)) sourceMessage += $"\nCreated by: {createdBy}";
                if (root.TryGetProperty("CreatedAtUtc", out var createdAtElement) && createdAtElement.TryGetDateTime(out var createdAt)) sourceMessage += $"\nTime: {createdAt:u}";
                sourceMessage = sourceKind.Equals("Achievement", StringComparison.OrdinalIgnoreCase)
                    ? $"🏆 {sourceMessage}\n\nUse `/achievement request` or press **Request Achievement**."
                    : $"💰 {sourceMessage}\n\nUse `/dkp request` or press **Request DKP**.";
                return string.IsNullOrWhiteSpace(description) ? sourceMessage : $"{sourceMessage}\nDescription: {description}";
            }
            if (notificationType == "ShopItem")
            {
                var name = Safe(GetString(root, "Name") ?? "Shop item");
                var description = Safe(GetString(root, "Description"));
                var price = root.TryGetProperty("Price", out var priceElement) ? priceElement.GetInt32() : 0;
                var limits = new List<string>();
                AddLimit(root, limits, "MaxPerUser", "Maximum per user");
                AddLimit(root, limits, "MaxActivePerUser", "Maximum active per user");
                AddLimit(root, limits, "MaxPerDay", "Maximum per day");
                AddLimit(root, limits, "MaxPerWeek", "Maximum per week");
                AddLimit(root, limits, "MaxPerMonth", "Maximum per month");
                var sharedKey = Safe(GetString(root, "PurchaseLimitKey"));
                if (!string.IsNullOrWhiteSpace(sharedKey)) limits.Add($"Shared limit group: **{sharedKey}**");
                var limitText = limits.Count == 0 ? string.Empty : $"\n📏 **Limits**\n• {string.Join("\n• ", limits)}";
                var requirementText = root.TryGetProperty("AchievementRequirements", out var requirementElement) && requirementElement.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", requirementElement.EnumerateArray().Select(x => Safe(x.GetString())))
                    : string.Empty;
                var requirementLine = string.IsNullOrWhiteSpace(requirementText) ? string.Empty : $"\n🏆 **Achievement requirements**\n• {requirementText}";
                var shopMessage = $"🛒 **NEW DKP SHOP ITEM**\n**{name}**\n{description}{requirementLine}\n💰 Price: **{price} DKP each**{limitText}\nUse `/shop buy` or press **Buy shop item** to check your personal availability and purchase it.";
                return shopMessage.Trim();
            }
            if (notificationType == "BulletinClaimCreated")
            {
                var title = Safe(GetString(root, "Title") ?? "Bulletin item");
                var claimUser = Safe(GetString(root, "UserName") ?? "Unknown member");
                var claimQuantity = root.TryGetProperty("Quantity", out var claimQuantityElement) ? claimQuantityElement.GetInt32() : 1;
                var dkp = root.TryGetProperty("DkpAmount", out var dkpElement) ? dkpElement.GetInt32() : 0;
                var claimMessage = $"🛡️ **NEW BULLETIN CLAIM**\n\nPlayer: **{claimUser}**\nItem: **{title}**\nQuantity: **{claimQuantity}**\nDKP reward: **{dkp}**";
                claimMessage += bulletinCommandId is ulong commandId
                    ? $"\n\nApprove with </bulletin approve:{commandId}> or press **Approve Bulletin Claim**."
                    : "\n\nApprove with `/bulletin approve` or press **Approve Bulletin Claim**.";
                return claimMessage;
            }
            if (notificationType == "DkpAwardRequestCreated")
            {
                var requestKind = Safe(GetString(root, "Kind") ?? "DKP");
                var icon = requestKind.Equals("Achievement", StringComparison.OrdinalIgnoreCase) ? "🏆" : "💰";
                var requestUser = Safe(GetString(root, "UserName") ?? "Unknown user");
                var requestSource = Safe(GetString(root, "SourceName") ?? "Unknown source");
                var requestAmount = root.TryGetProperty("Amount", out var requestAmountElement) ? requestAmountElement.GetInt32() : 0;
                var requestQuantity = root.TryGetProperty("Quantity", out var requestQuantityElement) ? requestQuantityElement.GetInt32() : 1;
                var requestComment = Safe(GetString(root, "Comment"));
                var requestMessage = $"{icon} **NEW {requestKind.ToUpperInvariant()} REQUEST**\n\nPlayer: **{requestUser}**\nSource: **{requestSource}**\nAmount: **{requestAmount} DKP**\nQuantity: **{requestQuantity}**";
                requestMessage += dkpCommandId is ulong commandId
                    ? $"\n\nReview with </dkp review:{commandId}> or press **Review DKP Request**."
                    : "\n\nReview with `/dkp review` or press **Review DKP Request**.";
                return string.IsNullOrWhiteSpace(requestComment) ? requestMessage : $"{requestMessage}\nComment: {requestComment}";
            }
            var kind = Safe(GetString(root, "Kind") ?? "DKP");
            var user = Safe(GetString(root, "UserName") ?? "Unknown user");
            var source = Safe(GetString(root, "SourceName") ?? "Unknown source");
            var amount = root.TryGetProperty("Amount", out var amountElement) ? amountElement.GetInt32() : 0;
            var quantity = root.TryGetProperty("Quantity", out var quantityElement) ? quantityElement.GetInt32() : 1;
            var comment = Safe(GetString(root, "Comment"));
            var message = $"💰 **NEW {kind.ToUpperInvariant()} REQUEST**\nPlayer: {user}\nSource: {source}\nAmount: {amount}\nQuantity: {quantity}";
            return string.IsNullOrWhiteSpace(comment) ? message : $"{message}\nComment: {comment}";
        }
        catch (JsonException)
        {
            return "**New DKP request**\nA new request is awaiting officer review.";
        }
    }

    private async Task<IMessageChannel?> GetNotificationChannelAsync(string notificationType, string payload)
    {
        var isMemberNotification = notificationType is "DkpSourceCreated" or "BulletinItem" or "ShopItem";
        if (isMemberNotification && settings.MemberNotificationMode == "forum")
        {
            var threadId = MemberThreadId(notificationType, payload);
            if (threadId is not ulong configuredThreadId || client is null) return null;
            if (client.GetChannel(configuredThreadId) is IMessageChannel cachedChannel) return cachedChannel;
            try
            {
                return await client.Rest.GetChannelAsync(configuredThreadId) as IMessageChannel;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to resolve configured member forum thread {ThreadId}.", configuredThreadId);
                return null;
            }
        }

        var channelId = isMemberNotification
            ? settings.MemberNotificationChannelId
            : settings.NotificationChannelId;
        if (channelId is not ulong id || client?.GetChannel(id) is not IMessageChannel channel) return null;
        if (!isMemberNotification || channel is not ITextChannel parent) return channel;

        var threadName = ThreadName(notificationType, payload);
        if (threadName is null) return channel;

        var activeThreads = await parent.GetActiveThreadsAsync();
        var existing = activeThreads.FirstOrDefault(x => x.Name.Equals(threadName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;

        logger.LogInformation("Creating Discord member thread {ThreadName} in channel {ChannelId}.", threadName, parent.Id);
        return await parent.CreateThreadAsync(threadName, ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
    }

    private static string? ThreadName(string notificationType, string payload)
    {
        if (notificationType == "BulletinItem") return "Bulletin Board";
        if (notificationType == "ShopItem") return "Shop";
        if (notificationType == "DkpSourceCreated")
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                return GetString(document.RootElement, "Kind")?.Equals("Achievement", StringComparison.OrdinalIgnoreCase) == true
                    ? "Achievements"
                    : "Repeatable DKP";
            }
            catch (JsonException) { return "Repeatable DKP"; }
        }

        return null;
    }

    private ulong? MemberThreadId(string notificationType, string payload)
    {
        var threadName = ThreadName(notificationType, payload);
        if (settings.MemberForumChannelId is not null)
        {
            var key = threadName switch
            {
                "Bulletin Board" => "bulletin-board",
                "Achievements" => "achievements",
                "Repeatable DKP" => "dkp-sources",
                "Shop" => "shop",
                _ => null
            };
            if (key is not null && forumThreads.TryGetValue(key, out var resolvedThreadId)) return resolvedThreadId;
        }
        return threadName switch
        {
            "Bulletin Board" => settings.BulletinThreadId,
            "Achievements" => settings.AchievementsThreadId,
            "Repeatable DKP" => settings.DkpSourcesThreadId,
            "Shop" => settings.ShopThreadId,
            _ => null
        };
    }

    private static MessageComponent? BuildBulletinComponents(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("BulletinItemId", out var id) || !Guid.TryParse(id.GetString(), out var itemId)) return null;
            return new ComponentBuilder()
                .WithButton("Claim Bulletin Item", $"bulletin:claim:{itemId:D}", ButtonStyle.Primary)
                .Build();
        }
        catch (JsonException) { return null; }
    }

    private static MessageComponent? BuildBulletinClaimComponents(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("ClaimId", out var id) || !Guid.TryParse(id.GetString(), out var claimId)) return null;
            return new ComponentBuilder()
                .WithButton("Review Bulletin Claim", $"bulletin:review:{claimId:D}", ButtonStyle.Primary)
                .Build();
        }
        catch (JsonException) { return null; }
    }

    private static MessageComponent? BuildAwardRequestComponents(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("RequestId", out var id) || !Guid.TryParse(id.GetString(), out var requestId)) return null;
            return new ComponentBuilder()
                .WithButton("Review DKP Request", $"award:review:{requestId:D}", ButtonStyle.Primary)
                .Build();
        }
        catch (JsonException) { return null; }
    }

    private static MessageComponent? BuildSourceComponents(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("SourceId", out var id) || !Guid.TryParse(id.GetString(), out var sourceId)) return null;
            var kind = GetString(root, "Kind")?.Equals("Achievement", StringComparison.OrdinalIgnoreCase) == true ? "achievement" : "dkp";
            var label = kind == "achievement" ? "Request Achievement" : "Request DKP";
            return new ComponentBuilder().WithButton(label, $"source:request:{kind}:{sourceId:D}", ButtonStyle.Primary).Build();
        }
        catch (JsonException) { return null; }
    }

    private static MessageComponent? BuildShopComponents(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("ShopItemId", out var id) || !Guid.TryParse(id.GetString(), out var itemId)) return null;
            return new ComponentBuilder()
                .WithButton("Buy shop item", $"shop:buy:{itemId:D}", ButtonStyle.Primary)
                .Build();
        }
        catch (JsonException) { return null; }
    }

    private static string FormatPrivateNotification(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var action = Safe(GetString(root, "Action") ?? "updated");
            var kind = Safe(GetString(root, "Kind") ?? "DKP");
            var source = Safe(GetString(root, "SourceName") ?? GetString(root, "ItemName") ?? "item");
            var amount = root.TryGetProperty("Amount", out var amountElement) ? amountElement.GetInt32() : 0;
            var quantity = root.TryGetProperty("Quantity", out var quantityElement) ? quantityElement.GetInt32() : 1;
            var newBalance = 0;
            var hasNewBalance = root.TryGetProperty("NewBalance", out var balanceElement) && balanceElement.ValueKind == JsonValueKind.Number && balanceElement.TryGetInt32(out newBalance);
            var comment = Safe(GetString(root, "ReviewComment"));
            var reviewer = Safe(GetString(root, "ReviewerName") ?? GetString(root, "OfficerName"));
            var icon = action.Equals("approved", StringComparison.OrdinalIgnoreCase) ? "✅" : "❌";
            var message = $"{icon} **{kind} request {action}**\nSource: **{source}**\nAmount: **{amount} DKP**\nQuantity: **{quantity}**";
            if (action.Equals("approved", StringComparison.OrdinalIgnoreCase) && hasNewBalance) message += $"\nNew balance: **{newBalance} DKP**";
            if (!string.IsNullOrWhiteSpace(reviewer)) message += $"\nProcessed by: **{reviewer}**";
            if (!string.IsNullOrWhiteSpace(comment)) message += $"\nComment: {comment}";
            return message;
        }
        catch (JsonException)
        {
            return "Your DKP request was processed. Please check the DKP website for details.";
        }
    }

    private static string? GetString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static void AddLimit(JsonElement root, List<string> limits, string property, string label)
    {
        if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var amount) && amount > 0)
            limits.Add($"{label}: **{amount}**");
    }

    private static string Safe(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("@", "@\u200b", StringComparison.Ordinal);

    private async Task RegisterCommandsAsync()
    {
        if (client is null || interactions is null) return;
        await registrationLock.WaitAsync();
        try
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await interactions.RegisterCommandsToGuildAsync(settings.GuildId, true);
                    if (client.GetGuild(settings.GuildId) is { } guild)
                    {
                        var registered = await guild.GetApplicationCommandsAsync();
                        bulletinCommandId = registered.FirstOrDefault(x => x.Name.Equals("bulletin", StringComparison.OrdinalIgnoreCase))?.Id;
                        dkpCommandId = registered.FirstOrDefault(x => x.Name.Equals("dkp", StringComparison.OrdinalIgnoreCase))?.Id;
                    }
                    logger.LogInformation("Registered Discord commands for guild {GuildId}.", settings.GuildId);
                    if (settings.MemberNotificationMode == "forum")
                    {
                        try { await EnsureForumSectionsAsync(CancellationToken.None); }
                        catch (Exception exception) { logger.LogWarning(exception, "Initial Discord forum section synchronization failed."); }
                    }
                    return;
                }
                catch (Exception exception)
                {
                    if (attempt == 3)
                    {
                        logger.LogError(exception, "Unable to register Discord commands for guild {GuildId} after 3 attempts.", settings.GuildId);
                        return;
                    }

                    logger.LogWarning(exception, "Unable to register Discord commands for guild {GuildId}; retry {Attempt}/3.", settings.GuildId, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }
        }
        finally { registrationLock.Release(); }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        if (interactions is null || services is null || client is null) return;
        var context = new SocketInteractionContext(client, interaction);
        try { await interactions.ExecuteCommandAsync(context, services); }
        catch (Exception exception) { logger.LogError(exception, "Discord interaction failed."); }
    }

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is null) logger.LogInformation("Discord: {Message}", message.Message);
        else logger.LogWarning(message.Exception, "Discord: {Message}", message.Message);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (notificationCts is not null)
        {
            notificationCts.Cancel();
            if (notificationTask is not null)
            {
                try { await notificationTask; }
                catch (OperationCanceledException) { }
            }
            notificationCts.Dispose();
            notificationCts = null;
            notificationTask = null;
        }
        if (forumSyncTask is not null)
        {
            try { await forumSyncTask; }
            catch (OperationCanceledException) { }
            forumSyncTask = null;
        }
        if (client is not null)
        {
            await client.StopAsync();
            await client.LogoutAsync();
        }
    }

    public void Dispose()
    {
        services?.Dispose();
        client?.Dispose();
        registrationLock.Dispose();
        forumSyncLock.Dispose();
    }
}
