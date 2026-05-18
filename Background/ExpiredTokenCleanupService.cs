using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Storage;

namespace SecureAuth.Background;

public sealed class ExpiredTokenCleanupService : BackgroundService
{
    private readonly InMemoryTokenStore _tokenStore;
    private readonly TimeSpan _interval;

    public ExpiredTokenCleanupService(InMemoryTokenStore tokenStore, IOptions<SecurityOptions> options)
    {
        _tokenStore = tokenStore;
        _interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.CleanupIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _tokenStore.RemoveExpired(DateTimeOffset.UtcNow);
        }
    }
}
