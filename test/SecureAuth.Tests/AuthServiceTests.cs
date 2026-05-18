using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Models;
using SecureAuth.Services;
using SecureAuth.Storage;
using Xunit;

namespace SecureAuth.Tests;

public sealed class AuthServiceTests
{
    private const string DemoPassword = "DemoPassword123!";
    private const string DemoPasswordHash = "pbkdf2-sha256:100000:U2VjdXJlQXV0aERlbW9TYWx0IQ==:xcasx8Cj6dYO5ghT/nPk266+Bj3JVNA72nlDzZLsPNQ=";

    [Fact]
    public void Login_ReturnsSimpleToken_ForValidCredentials()
    {
        var service = CreateAuthService();

        var response = service.Login("demo", DemoPassword);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ExchangeSimpleToken_ConsumesTokenAfterFirstUse()
    {
        var service = CreateAuthService();
        var loginResponse = service.Login("demo", DemoPassword);

        Assert.NotNull(loginResponse);

        var firstExchange = service.ExchangeSimpleToken(loginResponse.Token);
        var secondExchange = service.ExchangeSimpleToken(loginResponse.Token);

        Assert.NotNull(firstExchange);
        Assert.Null(secondExchange);
    }

    [Fact]
    public void Logout_ReturnsFalse_ForExpiredFullToken()
    {
        var tokenStore = new InMemoryTokenStore();
        var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds();
        tokenStore.Add(new StoredToken("expired-token", TokenKind.Full, expiredAt));

        var removed = tokenStore.TryRemoveFullToken("expired-token", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Assert.False(removed);
    }

    private static AuthService CreateAuthService()
    {
        var options = Options.Create(new SecurityOptions
        {
            StaticKey = "test-static-key",
            RequestFreshnessMinutes = 5,
            SimpleTokenTtlMinutes = 5,
            FullTokenTtlHours = 24,
            CleanupIntervalMinutes = 1,
            SeedUsers =
            [
                new SeedUserOptions
                {
                    Login = "demo",
                    PasswordHash = DemoPasswordHash
                }
            ]
        });

        return new AuthService(
            new InMemoryUserStore(options),
            new InMemoryTokenStore(),
            options,
            NullLogger<AuthService>.Instance);
    }
}
