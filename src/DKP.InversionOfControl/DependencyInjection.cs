using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using DKP.Application.Authentication;
using DKP.Application.Characters;
using DKP.Application.DkpTransactions;
using DKP.Application.Persistence;
using DKP.Application.Users;
using DKP.Facade.Contracts;
using DKP.Application.Shop;
using DKP.Application.Presets;
using DKP.Application.DkpAwardRequests;
using DKP.Application.LootReserve;
using DKP.Application.Achievements;

using DKP.Facade.Commands;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using DKP.InversionOfControl.Authentication;

namespace DKP.InversionOfControl;

public static class DependencyInjection
{
	public static IServiceCollection AddDkp(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException(
				"Missing ConnectionStrings:DefaultConnection. Configure it with User Secrets or the ConnectionStrings__DefaultConnection environment variable.");
		}

		var clientId = configuration["Discord:ClientId"];
		var clientSecret = configuration["Discord:ClientSecret"];
		if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
		{
			throw new InvalidOperationException(
				"Missing Discord:ClientId or Discord:ClientSecret. Configure both with User Secrets or environment variables.");
		}
		var guildId = configuration["Discord:GuildId"];
		if (string.IsNullOrWhiteSpace(guildId) || !ulong.TryParse(guildId, out _))
		{
			throw new InvalidOperationException(
				"Missing or invalid Discord:GuildId. Configure the Discord server ID with User Secrets or the Discord__GuildId environment variable.");
		}

		services.SetupDatabase(connectionString);
		services.AddSingleton(TimeProvider.System);
        services.AddScoped<CommandUnitOfWork>();
        services.AddScoped<ICommandUnitOfWork>(sp => sp.GetRequiredService<CommandUnitOfWork>());
        services.AddScoped<CommandContext>();
        services.AddScoped<ICurrentUser, AuthenticatedCurrentUser>();
        services.AddScoped<QuerySession>();
        services.AddScoped<IGuildActivityQueries, GuildActivityQueries>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<ICharacterRepository, CharacterRepository>();
		services.AddScoped<IShopRepository, ShopRepository>();
		services.AddScoped<IPresetRepository, PresetRepository>();
		services.AddScoped<IDkpAwardRequestRepository, DkpAwardRequestRepository>();
		services.AddScoped<IGuildSettingsRepository, GuildSettingsRepository>();
		services.AddScoped<IEventLedgerRepository, EventLedgerRepository>();
		services.AddScoped<IEventProjectionRebuilder, EventProjectionRebuilder>();
		services.AddScoped<IAccountQueries, AccountQueries>();
		services.AddScoped<IDkpQueries, DkpQueries>();
		services.AddScoped<IGuildMemberQueries, GuildMemberQueries>();
		services.AddScoped<IPlayerDetailsQueries, PlayerDetailsQueries>();
		services.AddScoped<IOfficerIdentityPolicy, OfficerIdentityPolicy>();
		services.AddScoped<IUserProvisioningService, UserProvisioningService>();
		services.AddScoped<ICharacterCommands, CharacterCommandService>();
		services.AddScoped<IDkpTransactionCommands, DkpTransactionCommandService>();
		services.AddScoped<IUserRoleCommands, UserRoleCommandService>();
		services.AddScoped<IUserBlockCommands, UserBlockCommandService>();
		services.AddScoped<IUserAdministrationQueries, UserAdministrationQueries>();
		services.AddScoped<IShopQueries, ShopQueries>();
		services.AddScoped<IShopPurchaseQueries, ShopPurchaseQueries>();
		services.AddScoped<IShopCommands, ShopCommandService>();
		services.AddScoped<IDkpPresetQueries, DkpPresetQueries>();
		services.AddScoped<IDkpPresetCommands, DkpPresetCommandService>();
		services.AddScoped<IDkpAwardRequestQueries, DkpAwardRequestQueries>();
		services.AddScoped<IDkpAwardRequestCommands, DkpAwardRequestCommandService>();
		services.AddScoped<ILootReserveQueries, LootReserveQueries>();
		services.AddScoped<ILootReserveCommands, LootReserveCommandService>();
		services.AddScoped<ILootReserveConsumptionCommands, LootReserveConsumptionCommandService>();
		services.AddScoped<ILootReserveConsumptionQueries, LootReserveConsumptionQueries>();
		services.AddScoped<ILootReserveModifierCommands, LootReserveModifierCommandService>();
		services.AddScoped<ILootReserveModifierQueries, LootReserveModifierQueries>();
		services.AddScoped<IAchievementCommands, AchievementCommandService>();
		services.AddScoped<IAchievementQueries, AchievementQueries>();
		services.AddScoped<IAchievementRepository, AchievementRepository>();

		services.AddAuthorization(options => options.AddPolicy("OfficerOnly", policy => policy.RequireRole("Officer")));
		services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = "Discord";
		})
		.AddCookie(options =>
		{
			options.LoginPath = "/account/login";
			options.Events = new CookieAuthenticationEvents
			{
				OnValidatePrincipal = async context =>
				{
					var discordId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
					await using var db = await context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<DkpDbContext>>().CreateDbContextAsync(context.HttpContext.RequestAborted);
					var user = string.IsNullOrWhiteSpace(discordId)
						? null
						: await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, context.HttpContext.RequestAborted);
					if (user is null || user.IsBlocked)
					{
						context.RejectPrincipal();
						await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
					}
				}
			};
		})
		.AddOAuth("Discord", options =>
		{
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/signin-discord";
			options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
			options.TokenEndpoint = "https://discord.com/api/oauth2/token";
			options.UserInformationEndpoint = "https://discord.com/api/users/@me";
			options.Scope.Add("identify");
			options.Scope.Add("guilds");
			options.SaveTokens = false;
			options.Events = new OAuthEvents
			{
				OnRedirectToAuthorizationEndpoint = context =>
				{
					// Discord users who authorized the application before the guilds scope
					// was added must explicitly approve the expanded scope set once.
					var authorizationUri = QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "consent");
					context.Response.Redirect(authorizationUri);
					return Task.CompletedTask;
				},
				OnRemoteFailure = context =>
				{
					var message = context.Failure?.Message ?? string.Empty;
					var reason = context.HttpContext.Items["DkpAuthenticationFailureReason"] as string
						?? (message.Contains("member of the configured Discord server", StringComparison.OrdinalIgnoreCase)
							? "guild"
							: message.Contains("blocked", StringComparison.OrdinalIgnoreCase)
								? "blocked"
								: "authentication");
					context.Response.Redirect($"/account/access-denied?reason={Uri.EscapeDataString(reason)}");
					context.HandleResponse();
					return Task.CompletedTask;
				},
				OnCreatingTicket = async context =>
				{
					using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
					using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
					response.EnsureSuccessStatusCode();
					using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
					var root = document.RootElement;
					var discordId = root.GetProperty("id").GetString()!;
					var discordName = root.TryGetProperty("global_name", out var globalName) && globalName.ValueKind != JsonValueKind.Null
						? globalName.GetString()!
						: root.GetProperty("username").GetString()!;
					var avatarHash = root.TryGetProperty("avatar", out var avatar) && avatar.ValueKind != JsonValueKind.Null
						? avatar.GetString()
						: null;
					var avatarUrl = avatarHash is null ? null : $"https://cdn.discordapp.com/avatars/{discordId}/{avatarHash}.png";

					using var guildRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me/guilds");
					guildRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
					using var guildResponse = await context.Backchannel.SendAsync(guildRequest, context.HttpContext.RequestAborted);
					if (!guildResponse.IsSuccessStatusCode)
					{
						FailAuthentication(context, "authentication");
						return;
					}
					using var guildsDocument = JsonDocument.Parse(await guildResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
					var isGuildMember = guildsDocument.RootElement.EnumerateArray().Any(guild => guild.TryGetProperty("id", out var id) && id.GetString() == guildId);
					if (!isGuildMember)
					{
						FailAuthentication(context, "guild");
						return;
					}

					await using var loginDb = await context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<DkpDbContext>>().CreateDbContextAsync(context.HttpContext.RequestAborted);
                    var existingUser = await loginDb.Users.AsNoTracking().SingleOrDefaultAsync(x => x.DiscordId == discordId, context.HttpContext.RequestAborted);
					if (existingUser?.IsBlocked == true)
					{
						FailAuthentication(context, "blocked");
						return;
					}

					var user = await context.HttpContext.RequestServices
						.GetRequiredService<IUserProvisioningService>()
						.ProvisionAsync(new DiscordUserProfile(discordId, discordName, avatarUrl), context.HttpContext.RequestAborted);
					if (user.IsBlocked)
					{
						FailAuthentication(context, "blocked");
						return;
					}

					var identity = context.Identity!;
					identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.DiscordId));
					identity.AddClaim(new Claim(ClaimTypes.Name, user.DiscordName));
					identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
					if (user.AvatarUrl is not null)
					{
						identity.AddClaim(new Claim("avatar_url", user.AvatarUrl));
					}
				}
			};
		});

		return services;
	}

	private static void FailAuthentication(OAuthCreatingTicketContext context, string reason)
	{
		context.HttpContext.Items["DkpAuthenticationFailureReason"] = reason;
		context.Response.Redirect($"/account/access-denied?reason={Uri.EscapeDataString(reason)}");
		context.NoResult();
	}

	private static IServiceCollection SetupDatabase(this IServiceCollection services, string connectionString)
	{
		services.AddDbContextFactory<DkpDbContext>(options =>
		{
			options.UseNpgsql(
				connectionString,
				npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(DkpDbContext).Assembly.GetName().Name));
		});

		return services;
	}

	public static WebApplication SetupDatabaseOnColdStart(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<DkpDbContext>();
		var knownMigrations = dbContext.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
        if (dbContext.Database.GetAppliedMigrations().Any(migration => !knownMigrations.Contains(migration)))
            throw new InvalidOperationException("Slice 12a requires a new empty database. The previous migration chain is not compatible. Back up and reset the database manually; no data has been deleted.");
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToArray();

		if (pendingMigrations.Length == 0)
		{
			Console.WriteLine("No pending database migrations found.");
			return app;
		}

		Console.WriteLine($"Applying {pendingMigrations.Length} pending database migration(s)...");
		dbContext.Database.Migrate();
		Console.WriteLine("Database migrations applied successfully.");

		return app;
	}
}
