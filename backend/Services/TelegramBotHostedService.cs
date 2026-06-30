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
        ILogger<TelegramBotHostedService> logger
    )
    {
        _config = config;
        _authHandler = authHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botToken = _config["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Telegram bot token is not configured; bot login is disabled.");
            return;
        }

        var bot = new TelegramBotClient(botToken);
        var receiverOptions = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };

        _logger.LogInformation("Starting Telegram bot polling for login flow.");

        await bot.ReceiveAsync(
            (_, update, ct) => _authHandler.HandleUpdateAsync(bot, update, ct),
            (_, exception, _) =>
            {
                _logger.LogError(exception, "Telegram bot polling error.");
                return Task.CompletedTask;
            },
            receiverOptions,
            stoppingToken
        );
    }
}
