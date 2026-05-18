using SecureAuth.Models;
using SecureAuth.Storage;
using Xunit;

namespace SecureAuth.Tests;

public sealed class InMemoryTokenStoreConcurrencyTests
{
    [Fact]
    public async Task TryConsumeSimpleToken_AllowsOnlyOneSuccessfulConsumer()
    {
        var tokenStore = new InMemoryTokenStore();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();
        tokenStore.Add(new StoredToken("shared-token", TokenKind.Simple, expiresAt));

        var attempts = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => tokenStore.TryConsumeSimpleToken("shared-token", DateTimeOffset.UtcNow)))
            .ToArray();

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(result => result));
    }
}
