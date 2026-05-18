using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Config;

public sealed class SeedUserOptions
{
    [Required]
    [MinLength(1)]
    public required string Login { get; init; }

    [Required]
    [MinLength(1)]
    public required string PasswordHash { get; init; }
}
