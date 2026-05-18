using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LoginRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public string? Login { get; init; }

    [Required]
    [MinLength(1)]
    public string? Password { get; init; }

    [Required]
    [MinLength(1)]
    public string? ApiSignature { get; init; }

    [Required]
    public long? RequestDate { get; init; }
}
