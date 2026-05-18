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
        return TryRemoveToken(value, TokenKind.Simple, now);
    }

    public bool TryRemoveFullToken(string value, DateTimeOffset now)
    {
        return TryRemoveToken(value, TokenKind.Full, now);
    }

    public int RemoveExpired(DateTimeOffset now)
    {
        var removed = 0;

        foreach (var pair in _tokens)
        {
            if (IsExpired(pair.Value, now) && RemoveIfUnchanged(pair.Key, pair.Value))
            {
                removed++;
            }
        }

        return removed;
    }

    private bool TryRemoveToken(string value, TokenKind expectedKind, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(value) || !_tokens.TryGetValue(value, out var token))
        {
            return false;
        }

        if (token.Kind != expectedKind)
        {
            return false;
        }

        if (IsExpired(token, now))
        {
            RemoveIfUnchanged(value, token);
            return false;
        }

        return RemoveIfUnchanged(value, token);
    }

    private bool RemoveIfUnchanged(string value, StoredToken token)
    {
        return ((ICollection<KeyValuePair<string, StoredToken>>)_tokens)
            .Remove(new KeyValuePair<string, StoredToken>(value, token));
    }

    private static bool IsExpired(StoredToken token, DateTimeOffset now) => token.ExpiresAt <= now;
}
