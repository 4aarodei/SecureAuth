using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Contracts;
using SecureAuth.Models;
using SecureAuth.Storage;

namespace SecureAuth.Services;

public sealed class AuthService
{
    private readonly InMemoryUserStore _userStore;
    private readonly InMemoryTokenStore _tokenStore;
    private readonly SecurityOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        InMemoryUserStore userStore,
        InMemoryTokenStore tokenStore,
        IOptions<SecurityOptions> options,
        ILogger<AuthService> logger)
    {
        _userStore = userStore;
        _tokenStore = tokenStore;
        _options = options.Value;
        _logger = logger;
    }

    public TokenResponse? Login(string login, string password)
    {
        if (!_userStore.TryGetByLogin(login, out var user) ||
            !PasswordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for user '{Login}'.", login);
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        _logger.LogInformation("Issued simple token for user '{Login}'.", login);

        return CreateToken(TokenKind.Simple, now.AddMinutes(_options.SimpleTokenTtlMinutes));
    }

    public TokenResponse? ExchangeSimpleToken(string simpleToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_tokenStore.TryConsumeSimpleToken(simpleToken, now))
        {
            _logger.LogWarning("Simple token exchange failed.");
            return null;
        }

        _logger.LogInformation("Issued full token after successful simple token exchange.");
        return CreateToken(TokenKind.Full, now.AddHours(_options.FullTokenTtlHours));
    }

    public bool Logout(string fullToken)
    {
        var removed = _tokenStore.TryRemoveFullToken(fullToken, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        if (removed)
        {
            _logger.LogInformation("Full token logout completed.");
        }
        else
        {
            _logger.LogWarning("Full token logout failed.");
        }

        return removed;
    }

    private TokenResponse CreateToken(TokenKind kind, DateTimeOffset expiresAt)
    {
        var value = TokenGenerator.Generate();
        _tokenStore.Add(new StoredToken(value, kind, expiresAt.ToUnixTimeMilliseconds()));

        return new TokenResponse(value, expiresAt);
    }
}
