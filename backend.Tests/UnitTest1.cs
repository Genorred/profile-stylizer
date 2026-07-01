namespace backend.Tests;

public class TelegramLoginSessionStoreTests
{
    [Fact]
    public void CreateAndComplete_ShouldStoreSessionAndExposeCompletedData()
    {
        var store = new TelegramLoginSessionStore();

        var session = store.Create(TimeSpan.FromMinutes(5));

        Assert.NotNull(session);
        Assert.False(session.Completed);
        Assert.NotEmpty(session.Token);
        Assert.True(session.ExpiresAt > DateTime.UtcNow);

        var fetchedSession = store.Get(session.Token);

        Assert.NotNull(fetchedSession);
        Assert.Equal(session.Token, fetchedSession!.Token);

        store.Complete(session.Token, "access-token", 42);

        Assert.True(fetchedSession.Completed);
        Assert.Equal("access-token", fetchedSession.AccessToken);
        Assert.Equal(42, fetchedSession.UserId);
    }
}
