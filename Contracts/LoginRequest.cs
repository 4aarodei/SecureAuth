using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LoginRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public string Login { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ApiSignature { get; init; } = string.Empty;

    public long RequestDate { get; init; }
}
