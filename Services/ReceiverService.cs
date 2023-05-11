using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TotkBot.Services;

public class ReceiverService : IReceiverService
{
    private readonly ILogger<ReceiverService> _logger;
    private readonly IUpdateHandler _updateHandler;
    private readonly ITelegramBotClient _botClient;

    public ReceiverService(ILogger<ReceiverService> logger,
        IUpdateHandler updateHandler,
        ITelegramBotClient botClient)
    {
        _logger = logger;
        _updateHandler = updateHandler;
        _botClient = botClient;
    }
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true
        };

        await _botClient.ReceiveAsync(
            updateHandler: _updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }
}
