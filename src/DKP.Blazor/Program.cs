using DKP.Blazor.Components;
using DKP.InversionOfControl;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Radzen;

namespace DKP.Blazor
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddDkp(builder.Configuration);
			builder.Services.AddRadzenComponents();
			builder.Services.AddRazorComponents()
				.AddInteractiveServerComponents();

			var app = builder.Build();
			app.SetupDatabaseOnColdStart();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
			app.UseHttpsRedirection();
			app.UseAuthentication();
			app.UseAuthorization();

			app.UseAntiforgery();

			app.MapGet("/account/login", (HttpContext context) =>
				Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, ["Discord"]));
			app.MapGet("/account/logout", async (HttpContext context) =>
			{
				await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				return Results.Redirect("/");
			});

			app.MapStaticAssets();
			app.MapRazorComponents<App>()
				.AddInteractiveServerRenderMode();

			app.Run();
		}
	}
}
