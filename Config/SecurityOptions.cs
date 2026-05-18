namespace SecureAuth.Config;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string StaticKey { get; init; } = string.Empty;

    public int RequestFreshnessMinutes { get; init; } = 5;

    public int SimpleTokenTtlMinutes { get; init; } = 5;

    public int FullTokenTtlHours { get; init; } = 24;

    public int CleanupIntervalMinutes { get; init; } = 1;

    public IReadOnlyCollection<SeedUserOptions> SeedUsers { get; init; } = Array.Empty<SeedUserOptions>();
}

public sealed class SeedUserOptions
{
    public string Login { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;
}
