using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TotkBot.ConfigModel;
using TotkBot.HostedServices;
using TotkBot.Services;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: null)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSystemd()
        .UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: null);
        })
        .ConfigureServices((context, services) =>
        {
            services
                .AddOptions<BotConfiguration>()
                .Bind(context.Configuration.GetSection(BotConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart()
                ;

            services.AddFeatureManagement();

            services
                .AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((client, sp) =>
                {
                    var botConfig = sp.GetRequiredService<IOptions<BotConfiguration>>();
                    var options = new TelegramBotClientOptions(botConfig.Value.Token);
                    return new TelegramBotClient(options, client);
                });

            services.AddSingleton<IChatRepo, SqliteChatRepo>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddScoped<IUpdateHandler, UpdateHandler>();
            services.AddScoped<IReceiverService, ReceiverService>();
            services.AddHostedService<PollingService>();
            services.AddHostedService<MassUpdateService>();
        })
        .Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
