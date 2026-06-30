using System.Collections.Concurrent;
using System.Security.Cryptography;

public sealed class TelegramLoginSession
{
    public required string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool Completed { get; set; }
    public string? AccessToken { get; set; }
    public int? UserId { get; set; }
}

public sealed class TelegramLoginSessionStore
{
    private readonly ConcurrentDictionary<string, TelegramLoginSession> _sessions = new();

    public TelegramLoginSession Create(TimeSpan lifetime)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var session = new TelegramLoginSession
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
        };

        _sessions[token] = session;
        CleanupExpired();
        return session;
    }

    public TelegramLoginSession? Get(string token)
    {
        CleanupExpired();
        return _sessions.TryGetValue(token, out var session) ? session : null;
    }

    public void Complete(string token, string accessToken, int userId)
    {
        if (!_sessions.TryGetValue(token, out var session))
            return;

        session.Completed = true;
        session.AccessToken = accessToken;
        session.UserId = userId;
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var (token, session) in _sessions)
        {
            if (session.ExpiresAt < now && !session.Completed)
                _sessions.TryRemove(token, out _);
        }
    }
}
