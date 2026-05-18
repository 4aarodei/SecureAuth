using System.Collections.Concurrent;
using SecureAuth.Models;

namespace SecureAuth.Storage;

public sealed class InMemoryTokenStore
{
    private readonly ConcurrentDictionary<string, StoredToken> _tokens = new(StringComparer.Ordinal);

    public void Add(StoredToken token)
    {
        _tokens[token.Value] = token;
    }

    public bool TryConsumeSimpleToken(string value, DateTimeOffset now)
    {
        return TryRemoveToken(value, TokenKind.Simple, now.ToUnixTimeMilliseconds());
    }

    public bool TryRemoveFullToken(string value, long nowUnixTimeMilliseconds)
    {
        return TryRemoveToken(value, TokenKind.Full, nowUnixTimeMilliseconds);
    }

    public int RemoveExpired(long nowUnixTimeMilliseconds)
    {
        var removed = 0;

        foreach (var pair in _tokens)
        {
            if (IsExpired(pair.Value, nowUnixTimeMilliseconds) && RemoveIfUnchanged(pair.Key, pair.Value))
            {
                removed++;
            }
        }

        return removed;
    }

    private bool TryRemoveToken(string value, TokenKind expectedKind, long nowUnixTimeMilliseconds)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        while (_tokens.TryGetValue(value, out var token))
        {
            if (token.Kind != expectedKind)
            {
                return false;
            }

            if (!RemoveIfUnchanged(value, token))
            {
                continue;
            }

            return !IsExpired(token, nowUnixTimeMilliseconds);
        }

        return false;
    }

    private bool RemoveIfUnchanged(string value, StoredToken token)
    {
        return ((ICollection<KeyValuePair<string, StoredToken>>)_tokens)
            .Remove(new KeyValuePair<string, StoredToken>(value, token));
    }

    private static bool IsExpired(StoredToken token, long nowUnixTimeMilliseconds) =>
        token.ExpiresAtUnixTimeMilliseconds <= nowUnixTimeMilliseconds;
}
