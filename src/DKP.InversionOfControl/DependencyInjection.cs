using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using DKP.Application.Authentication;
using DKP.Application.Characters;
using DKP.Application.Persistence;
using DKP.Facade;
using DKP.Facade.Queries;
using DKP.Infrastructure.Persistence;
using DKP.Infrastructure.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

		services.SetupDatabase(configuration, connectionString);
		services.AddSingleton(TimeProvider.System);
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<ICharacterRepository, CharacterRepository>();
		services.AddScoped<IAccountQueries, AccountQueries>();
		services.AddScoped<IOfficerIdentityPolicy, OfficerIdentityPolicy>();
		services.AddScoped<IUserProvisioningService, UserProvisioningService>();
		services.AddScoped<ICharacterCommandService, CharacterCommandService>();
		services.AddScoped<IAccountFacade, AccountFacade>();

		services.AddAuthorization(options => options.AddPolicy("OfficerOnly", policy => policy.RequireRole("Officer")));
		services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = "Discord";
		})
		.AddCookie(options => options.LoginPath = "/account/login")
		.AddOAuth("Discord", options =>
		{
			options.ClientId = clientId;
			options.ClientSecret = clientSecret;
			options.CallbackPath = "/signin-discord";
			options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
			options.TokenEndpoint = "https://discord.com/api/oauth2/token";
			options.UserInformationEndpoint = "https://discord.com/api/users/@me";
			options.Scope.Add("identify");
			options.SaveTokens = false;
			options.Events = new OAuthEvents
			{
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

					var user = await context.HttpContext.RequestServices
						.GetRequiredService<IUserProvisioningService>()
						.ProvisionAsync(new DiscordUserProfile(discordId, discordName, avatarUrl), context.HttpContext.RequestAborted);

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

	private static IServiceCollection SetupDatabase(this IServiceCollection services, IConfiguration configuration, string connectionString)
	{
		services.AddDbContext<DkpDbContext>(options =>
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
