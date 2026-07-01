using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUser = Telegram.Bot.Types.User;

public sealed class TelegramBotAuthHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramLoginSessionStore _sessionStore;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramBotAuthHandler> _logger;

    public TelegramBotAuthHandler(
        IServiceScopeFactory scopeFactory,
        TelegramLoginSessionStore sessionStore,
        IConfiguration config,
        ILogger<TelegramBotAuthHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _sessionStore = sessionStore;
        _config = config;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken cancellationToken
    )
    {
        if (update.Message is not { Text: { } text } message || message.From is null)
            return;

        if (!text.StartsWith("/start", StringComparison.Ordinal))
            return;

        var payload = text.Length > "/start".Length
            ? text["/start".Length..].TrimStart()
            : string.Empty;

        if (!payload.StartsWith("login_", StringComparison.Ordinal))
            return;

        var token = payload["login_".Length..];
        var session = _sessionStore.Get(token);

        if (session is null)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Сессия входа не найдена или истекла. Вернитесь на сайт и попробуйте снова.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (session.Completed)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Эта ссылка для входа уже использована.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Сессия входа истекла. Вернитесь на сайт и начните вход заново.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var telegramUser = message.From;
        var telegramId = telegramUser.Id;

        try
        {
            var profile = await BuildTelegramProfileAsync(bot, telegramUser, cancellationToken);
            var accessToken = await UpsertUserAndCreateTokenAsync(telegramId, profile);

            _sessionStore.Complete(token, accessToken, profile.UserId);

            await bot.SendMessage(
                message.Chat.Id,
                "Вход выполнен. Вернитесь на сайт — окно должно обновиться автоматически.",
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Telegram login for user {TelegramId}", telegramId);
            await bot.SendMessage(
                message.Chat.Id,
                "Не удалось завершить вход. Попробуйте ещё раз позже.",
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task<TelegramProfileData> BuildTelegramProfileAsync(
        ITelegramBotClient bot,
        TelegramUser telegramUser,
        CancellationToken cancellationToken
    )
    {
        var name = BuildDisplayName(telegramUser);
        var username = telegramUser.Username ?? string.Empty;
        var bio = await TryGetBioAsync(bot, telegramUser.Id, cancellationToken);
        var photoPaths = await GetProfilePhotoPathsAsync(bot, telegramUser.Id, cancellationToken);

        return new TelegramProfileData
        {
            Name = name,
            Username = username,
            Bio = bio,
            PhotoPaths = photoPaths,
        };
    }

    private static string BuildDisplayName(TelegramUser telegramUser)
    {
        var parts = new[] { telegramUser.FirstName, telegramUser.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var name = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(name) ? $"User {telegramUser.Id}" : name;
    }

    private async Task<string> TryGetBioAsync(
        ITelegramBotClient bot,
        long telegramId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var chat = await bot.GetChat(new ChatId(telegramId), cancellationToken);
            var bio = chat.Bio?.Trim();

            if (!string.IsNullOrEmpty(bio))
                return bio;

            _logger.LogInformation("Telegram user {TelegramId} has no bio in profile", telegramId);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Telegram bio for user {TelegramId}", telegramId);
            return string.Empty;
        }
    }

    private async Task<List<string>> GetProfilePhotoPathsAsync(
    ITelegramBotClient bot,
    long telegramId,
    CancellationToken cancellationToken
)
{
    var photos = await bot.GetUserProfilePhotos(
        telegramId,
        offset: 0,
        limit: 3,
        cancellationToken: cancellationToken
    );

    var result = new List<string>();

    var botToken = _config["Telegram:BotToken"]; // IMPORTANT

    foreach (var photoSet in photos.Photos)
    {
        var photo = photoSet.Last(); // highest resolution

        var file = await bot.GetFile(photo.FileId, cancellationToken);

        if (string.IsNullOrWhiteSpace(file.FilePath))
            continue;

        var url =
            $"https://api.telegram.org/file/bot{botToken}/{file.FilePath}";

        result.Add(url);
    }

    return result;
}

    private async Task<string> UpsertUserAndCreateTokenAsync(long telegramId, TelegramProfileData profile)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        var syntheticEmail = $"tg{telegramId}@telegram.local";

        if (user is null)
        {
            user = new MyApp.Models.User
            {
                TelegramId = telegramId,
                Email = syntheticEmail,
                Role = "user",
            };
            db.Users.Add(user);
        }

        user.Name = profile.Name;
        user.Bio = profile.Bio;
        user.TelegramName = profile.Name;
        user.TelegramUsername = profile.Username;
        user.TelegramBio = profile.Bio;
        user.TelegramPictures = profile.PhotoPaths;

        await db.SaveChangesAsync();
        profile.UserId = user.Id;

        return CreateToken(user, _config);
    }

    private static string CreateToken(MyApp.Models.User user, IConfiguration config)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TelegramProfileData
    {
        public required string Name { get; init; }
        public required string Username { get; init; }
        public required string Bio { get; init; }
        public required List<string> PhotoPaths { get; init; }
        public int UserId { get; set; }
    }
}
