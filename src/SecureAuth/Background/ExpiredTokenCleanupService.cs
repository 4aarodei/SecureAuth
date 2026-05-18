using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Storage;

namespace SecureAuth.Background;

public sealed class ExpiredTokenCleanupService : BackgroundService
{
    private readonly InMemoryTokenStore _tokenStore;
    private readonly SecurityOptions _options;
    private readonly ILogger<ExpiredTokenCleanupService> _logger;

    public ExpiredTokenCleanupService(
        InMemoryTokenStore tokenStore,
        IOptions<SecurityOptions> options,
        ILogger<ExpiredTokenCleanupService> logger)
    {
        _tokenStore = tokenStore;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.CleanupIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var removed = _tokenStore.RemoveExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (removed > 0)
            {
                _logger.LogInformation("Removed {RemovedTokenCount} expired tokens.", removed);
            }
        }
    }
}
