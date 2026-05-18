using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class TokenRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public required string SimpleToken { get; init; }

    [Required]
    [MinLength(1)]
    public required string ApiSignature { get; init; }

    public long RequestDate { get; init; }
}
