using DKP.Domain;
using DKP.Facade.Commands;
using DKP.Facade.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DKP.Infrastructure.Persistence;

public sealed class DiscordForumSectionCommands(IDbContextFactory<DkpDbContext> factory, ICurrentUser currentUser, IConfiguration configuration, TimeProvider time) : IDiscordForumSectionCommands
{
    public async Task<DiscordForumSectionDto> UpdateContentAsync(string sectionKey, UpdateDiscordForumSectionRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureOfficerAsync(cancellationToken);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var guildId = configuration["Discord:GuildId"]?.Trim() is { Length: > 0 } configuredGuild
            ? configuredGuild
            : throw new InvalidOperationException("Discord:GuildId is not configured.");
        var section = await db.DiscordForumSections.SingleOrDefaultAsync(x => x.GuildId == guildId && x.SectionKey == sectionKey, cancellationToken);
        if (section is null)
        {
            section = new DiscordForumSection(guildId, sectionKey, DefaultContent(sectionKey), time.GetUtcNow().UtcDateTime);
            db.DiscordForumSections.Add(section);
        }
        section.SetContent(request.InitialMessageContent, time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return Map(section);
    }

    public async Task<DiscordForumSectionDto> RegisterAsync(RegisterDiscordForumSectionRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = time.GetUtcNow().UtcDateTime;
        var section = await db.DiscordForumSections.SingleOrDefaultAsync(x => x.GuildId == request.GuildId && x.SectionKey == request.SectionKey, cancellationToken);
        if (section is null)
        {
            section = new DiscordForumSection(request.GuildId, request.SectionKey, DiscordForumDefaults.Content(request.SectionKey), now);
            db.DiscordForumSections.Add(section);
        }
        else if (IsLegacyDefault(request.SectionKey, section.InitialMessageContent))
        {
            section.SetContent(DiscordForumDefaults.Content(request.SectionKey), now);
        }
        section.Register(request.ForumChannelId, request.ThreadId, request.InitialMessageId, now);
        await db.SaveChangesAsync(cancellationToken);
        return Map(section);
    }

    private async Task EnsureOfficerAsync(CancellationToken ct)
    {
        var id = await currentUser.GetDiscordIdAsync(ct);
        if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException("Login is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == id, ct);
        if (user is null || user.IsBlocked || user.Role != DKP.Domain.UserRole.Officer) throw new UnauthorizedAccessException("An active Officer is required.");
    }

    private static DiscordForumSectionDto Map(DiscordForumSection x) => new(x.Id, x.GuildId, x.SectionKey, x.ForumChannelId, x.ThreadId, x.InitialMessageId, x.InitialMessageContent, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static bool IsLegacyDefault(string key, string content)
    {
        if (content.Contains("Slash commands work in all Discord chats", StringComparison.OrdinalIgnoreCase)) return false;
        return key switch
        {
            "bulletin-board" => content == "📌 **Bulletin Board**\nAvailable guild bulletin items are posted here." || content.Contains("📌 **BULLETIN BOARD**", StringComparison.Ordinal),
            "achievements" => content == "🏆 **Achievements**\nAvailable achievements are posted here." || content.Contains("🏆 **ACHIEVEMENTS**", StringComparison.Ordinal),
            "dkp-sources" => content == "💰 **DKP Sources**\nAvailable DKP sources are posted here." || content.Contains("💰 **REPEATABLE DKP**", StringComparison.Ordinal),
            "shop" => content == "🛒 **DKP Shop**\nNew shop items are posted here." || content.Contains("🛒 **DKP SHOP**", StringComparison.Ordinal),
            "bot-help" => content == "🤖 **DKP Bot Help**\nUse the available slash commands to manage your characters, DKP and purchases." || content.Contains("🤖 **DKP BOT HELP**", StringComparison.Ordinal) && !content.Contains("/notifications toggle", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string DefaultContent(string key) => DiscordForumDefaults.Content(key);
    /* private static string LegacyDefaultContent(string key) => key switch
        "bulletin-board" => "📌 **BULLETIN BOARD**\n\nThis channel contains temporary guild tasks and offers. Each post explains the reward, available quantity and deadline.\n\nTo claim an item, use the button on the post or `/bulletin claim`. Your claim is reviewed by an Officer before the DKP is awarded.\n\nWebsite: <https://wowforever.coffeecottage.dk/bulletin>\n\n**First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.\n\nSuggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.\n\nYou can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.",
        "achievements" => "🏆 **ACHIEVEMENTS**\n\nThis channel shows achievements that can be earned for DKP. Read the requirement before requesting one. A request is reviewed by an Officer; it is not granted automatically.\n\nUse the button on an achievement post or `/achievement request` to submit a request. You can also review your achievements with `/achievements`.\n\nWebsite: <https://wowforever.coffeecottage.dk/my-dkp/sources>\n\n**First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.\n\nSuggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.\n\nYou can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.",
        "dkp-sources" => "💰 **REPEATABLE DKP**\n\nThis channel contains repeatable ways to earn DKP, such as approved dungeon or guild activities. Each post shows the amount and any lifetime limit.\n\nUse the button on a post or `/dkp request` to request DKP. An Officer must approve the request before your balance changes. `/dkp sources` shows your remaining opportunities.\n\nWebsite: <https://wowforever.coffeecottage.dk/my-dkp/sources>\n\n**First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.\n\nSuggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.\n\nYou can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.",
        "shop" => "🛒 **DKP SHOP**\n\nThis channel announces available shop items and their prices and purchase limits. Requirements and remaining availability are shown on each post.\n\nUse the button on an item or `/shop buy` to purchase it. Your personal limits, achievement requirements and DKP balance are checked before the purchase is completed. Use `/purchases` to see your purchases.\n\nWebsite: <https://wowforever.coffeecottage.dk/dkp-shop>\n\n**First time here?** Create your account on the website first, or use `/account create`, then create your characters and choose a main character. If you have questions, write in **DKP Bot Help** or contact <@333676904768864268>.\n\nSuggestions for improvements, new achievements, shop items or repeatable DKP sources are welcome in **DKP Bot Help** or via <@333676904768864268>.\n\nYou can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.",
        _ => "🤖 **DKP BOT HELP**\n\nThe DKP Bot gives guild members access to common DKP features directly in Discord. You can manage characters, view DKP, request repeatable DKP and achievements, claim Bulletin Board items, buy shop items and view your purchases.\n\n**Getting started**\n1. Open <https://wowforever.coffeecottage.dk/> and log in with Discord, or run `/account create`.\n2. Create your characters with `/characters create`.\n3. Choose your main with `/characters main`.\n4. Use `/help` whenever you need an overview of the commands.\n5. Use `/notifications toggle` to turn private review notifications on or off.\n\nRequests and claims are reviewed by Officers. A request does not change your DKP until it is approved. Shop purchases and limits are checked automatically.\n\n**Questions and suggestions**\nUse this DKP Bot Help post for general questions and suggestions, or contact <@333676904768864268> if you need Officer assistance. Suggestions for new achievements, shop items and repeatable DKP sources are especially welcome. Please keep questions about a specific request in the relevant channel.\n\nYou can use `/dkp balance` to see your current DKP balance. Slash commands work in all Discord chats where the bot has access.\n\nWebsite: <https://wowforever.coffeecottage.dk/>"
    }; */
}
