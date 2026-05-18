using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Config;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Required]
    [MinLength(1)]
    public required string StaticKey { get; init; }

    [Range(1, int.MaxValue)]
    public int RequestFreshnessMinutes { get; init; } = 5;

    [Range(1, int.MaxValue)]
    public int SimpleTokenTtlMinutes { get; init; } = 5;

    [Range(1, int.MaxValue)]
    public int FullTokenTtlHours { get; init; } = 24;

    [Range(1, int.MaxValue)]
    public int CleanupIntervalMinutes { get; init; } = 1;

    public IReadOnlyCollection<SeedUserOptions> SeedUsers { get; init; } = Array.Empty<SeedUserOptions>();
}
