using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TotkBot.ConfigModel;
using TotkBot.Services;

namespace TotkBot.HostedServices;

public class MassUpdateService : BackgroundService
{
    private readonly ILogger<MassUpdateService> _logger;
    private readonly IOptionsMonitor<BotConfiguration> _options;
    private readonly IChatRepo _repo;
    private readonly ILocalizationService _loc;
    private readonly ITelegramBotClient _bot;
    private readonly string[] _languages;

    public MassUpdateService(ILogger<MassUpdateService> logger,
        IOptionsMonitor<BotConfiguration> options, 
        IChatRepo repo,
        ILocalizationService localization,
        ITelegramBotClient bot)
    {
        _logger = logger;
        _options = options;
        _repo = repo;
        _loc = localization;
        _bot = bot;
        _languages = new[] { "it", "en" };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var delta = _options.CurrentValue.TargetDate - DateTime.UtcNow;
            delta = TimeSpan.FromSeconds(Math.Floor(delta.TotalSeconds));

            if (delta < TimeSpan.Zero)
            {
                break;
            }

            var triggered = delta switch
            {
                { TotalHours: >= 1, Minutes: 0, Seconds: 0 } => true,
                { TotalMinutes: < 30, Seconds: 0 } when delta.Minutes % 5 == 0 => true,
                { TotalMinutes: < 5, Seconds: 0 } => true,
                { TotalSeconds: <=5 and >=0 } => true,
                { TotalSeconds: < 0 } => false,

                _ => false
            };

            if (!triggered)
            {
                _logger.LogDebug("Skipping at {Delta}", delta);
                continue;
            }

            _logger.LogInformation("Triggering at {Delta}", delta);

            var messages = _languages.ToDictionary(
                l => l,
                l => _loc.GetCountdownMessage(l, delta)
            );

            foreach (var (chatId, lang) in _repo.EnumerateGroups())
            {
                var text = messages.TryGetValue(lang, out var txt) ? txt : messages["en"];
                await _bot.SendTextMessageAsync(chatId, text, 
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: stoppingToken);
            }
        }
    }
}