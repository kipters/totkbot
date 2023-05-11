using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TotkBot.ConfigModel;

namespace TotkBot.Services;

public partial class UpdateHandler : IUpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private ITelegramBotClient _bot;
    private readonly BotConfiguration _options;
    private readonly IChatRepo _repo;
    private readonly ILocalizationService _loc;

    public UpdateHandler(ILogger<UpdateHandler> logger,
        ITelegramBotClient botClient,
        IOptionsSnapshot<BotConfiguration> options,
        IChatRepo repo,
        ILocalizationService loc)
    {
        _logger = logger;
        _bot = botClient;
        _options = options.Value;
        _repo = repo;
        _loc = loc;
    }
    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error handling bot update");

        if (exception is RequestException)
        {
            // TODO replace with Polly
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // TODO replace with Channels or Dataflow?
        var handler = update switch
        {
            { Message: { } message } => HandleMessage(message, cancellationToken),
            { MyChatMember: { NewChatMember: { Status: ChatMemberStatus.Member } } joinStatus } => HandleGroupJoin(joinStatus, cancellationToken),
            { MyChatMember: { NewChatMember: { Status: ChatMemberStatus.Left } } joinStatus } => HandleGroupLeave(joinStatus, cancellationToken),
            { MyChatMember: { NewChatMember: { Status: ChatMemberStatus.Kicked } } joinStatus } => HandleGroupLeave(joinStatus, cancellationToken),
            _ => HandleUnknownUpdate(update, cancellationToken),
        };

        await handler;
    }

    private ValueTask HandleGroupJoin(ChatMemberUpdated status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Joined chat {ChatName} with ID {ChatId}", status.Chat.Title, status.Chat.Id);
        _repo.AddChat(status.Chat.Id, "en");
        return ValueTask.CompletedTask;
    }

    private ValueTask HandleGroupLeave(ChatMemberUpdated status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Left chat {ChatName} with ID {ChatId}", status.Chat.Title, status.Chat.Id);
        _repo.RemoveChat(status.Chat.Id);
        return ValueTask.CompletedTask;
    }

    [GeneratedRegex(@"^\/([a-zA-Z0-9]+)($|\@[a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CommandMessageRegex();
    private async ValueTask HandleMessage(Message message, CancellationToken cancellationToken)
    {
        var chatName = message.Chat.Title ?? message.Chat.Username ?? message.Chat.FirstName;
        _logger.LogInformation("Message on chat {ChatName} with ID {ChatId} [{Language}]", chatName, message.Chat.Id, message.From?.LanguageCode);
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        var match = CommandMessageRegex().Match(message.Text);

        if (match.Captures.Count == 0)
        {
            return;
        }

        var command = match.Groups[1].Value.Trim();
        var target = match.Groups[2].Value.Trim();
        var react = string.IsNullOrEmpty(target) || target == _options.BotName;

        if (!react)
        {
            return;
        }

        var commandHandler = command switch
        {
            "start" => HandleStartCommand(message, cancellationToken),
            "stop" => HandleStopCommand(message, cancellationToken),
            "countdown" => HandleCountdownCommand(message, cancellationToken),
            "spam" when message.From?.Id == _options.AdminId => DebugMassMessage(cancellationToken),
            _ => ValueTask.CompletedTask
        };

        await commandHandler;
    }

    private ValueTask HandleStopCommand(Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async ValueTask DebugMassMessage(CancellationToken cancellationToken)
    {
        var delta = _options.TargetDate - DateTime.UtcNow;
        var strings = new[] { "it", "en" }
            .ToDictionary(
                t => t,
                t => _loc.GetCountdownMessage(t, delta)
            );

        var count = 0;
        foreach (var (chatId, language) in _repo.EnumerateGroups())
        {
            var text = strings.TryGetValue(language, out var txt) ? txt : strings["en"];
            await _bot.SendTextMessageAsync(chatId, text,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            count++;
        }

        await _bot.SendTextMessageAsync(_options.AdminId, $"{count} sent",
            cancellationToken: cancellationToken);
    }

    private async ValueTask HandleCountdownCommand(Message message, CancellationToken cancellationToken)
    {
        var language = message.From?.LanguageCode;

        var delta = _options.TargetDate - DateTime.UtcNow;
        var text = _loc.GetCountdownMessage(language, delta);

        if (message.Chat.Type == ChatType.Private)
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, text,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, text,
                replyToMessageId: message.MessageId,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
        }
    }

    private async ValueTask HandleStartCommand(Message message, CancellationToken cancellationToken)
    {
        if (message.From is null || message.Chat.Type != ChatType.Private)
        {
            return;
        }

        var text = _loc.GetWelcomeMessage(message.From.LanguageCode);

        await _bot.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
    }

    private ValueTask HandleUnknownUpdate(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return ValueTask.CompletedTask;
    }
}
