using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LoginRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public required string Login { get; init; }

    [Required]
    [MinLength(1)]
    public required string Password { get; init; }

    [Required]
    [MinLength(1)]
    public required string ApiSignature { get; init; }

    public long RequestDate { get; init; }
}
