using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureAuth.Config;
using Xunit;

namespace SecureAuth.Tests;

public sealed class SecurityOptionsValidationTests
{
    [Fact]
    public void OptionsValidation_Throws_WhenStaticKeyMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SecurityOptions.SectionName}:RequestFreshnessMinutes"] = "5",
                [$"{SecurityOptions.SectionName}:SimpleTokenTtlMinutes"] = "5",
                [$"{SecurityOptions.SectionName}:FullTokenTtlHours"] = "24",
                [$"{SecurityOptions.SectionName}:CleanupIntervalMinutes"] = "1"
            })
            .Build();

        var services = new ServiceCollection();
        services
            .Configure<SecurityOptions>(configuration.GetRequiredSection(SecurityOptions.SectionName))
            .AddOptionsWithValidateOnStart<SecurityOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<SecurityOptions>>().Value);
    }
}
