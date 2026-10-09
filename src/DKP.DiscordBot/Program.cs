using DKP.DiscordBot;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<DkpApiClient>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<DiscordBotSettings>(_ => DiscordBotSettings.FromConfiguration(builder.Configuration));
builder.Services.AddHostedService<DiscordBotWorker>();
await builder.Build().RunAsync();
