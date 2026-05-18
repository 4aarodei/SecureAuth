using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Models;

namespace SecureAuth.Storage;

public sealed class InMemoryUserStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore(IOptions<SecurityOptions> options)
    {
        foreach (var user in options.Value.SeedUsers)
        {
            if (string.IsNullOrWhiteSpace(user.Login) || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                continue;
            }

            _users.TryAdd(user.Login, new UserRecord(user.Login, user.PasswordHash));
        }
    }

    public bool TryGetByLogin(string login, out UserRecord user)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            user = null!;
            return false;
        }

        return _users.TryGetValue(login, out user!);
    }
}
