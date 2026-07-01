using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public sealed class TelegramBotHostedService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly TelegramBotAuthHandler _authHandler;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(
        IConfiguration config,
        TelegramBotAuthHandler authHandler,
        ILogger<TelegramBotHostedService> logger)
    {
        _config = config;
        _authHandler = authHandler;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = _config["Telegram:BotToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Bot token is missing");
            return Task.CompletedTask;
        }

        var bot = new TelegramBotClient(token);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken
        );

        _logger.LogInformation("Bot started");

        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        await _authHandler.HandleUpdateAsync(bot, update, ct);
    }

    private Task HandleErrorAsync(
        ITelegramBotClient bot,
        Exception ex,
        CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram error");
        return Task.CompletedTask;
    }
}
