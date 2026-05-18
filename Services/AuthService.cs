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
    private readonly PasswordHasher _passwordHasher;
    private readonly TokenGenerator _tokenGenerator;
    private readonly SecurityOptions _options;

    public AuthService(
        InMemoryUserStore userStore,
        InMemoryTokenStore tokenStore,
        PasswordHasher passwordHasher,
        TokenGenerator tokenGenerator,
        IOptions<SecurityOptions> options)
    {
        _userStore = userStore;
        _tokenStore = tokenStore;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _options = options.Value;
    }

    public TokenResponse? Login(string login, string password)
    {
        if (!_userStore.TryGetByLogin(login, out var user) ||
            !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        return CreateToken(TokenKind.Simple, now.AddMinutes(_options.SimpleTokenTtlMinutes));
    }

    public TokenResponse? ExchangeSimpleToken(string simpleToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_tokenStore.TryConsumeSimpleToken(simpleToken, now))
        {
            return null;
        }

        return CreateToken(TokenKind.Full, now.AddHours(_options.FullTokenTtlHours));
    }

    public bool Logout(string fullToken)
    {
        return _tokenStore.TryRemoveFullToken(fullToken, DateTimeOffset.UtcNow);
    }

    private TokenResponse CreateToken(TokenKind kind, DateTimeOffset expiresAt)
    {
        var value = _tokenGenerator.Generate();
        _tokenStore.Add(new StoredToken(value, kind, expiresAt));

        return new TokenResponse(value, expiresAt);
    }
}
