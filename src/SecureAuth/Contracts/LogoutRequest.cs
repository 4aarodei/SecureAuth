using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LogoutRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public required string FullToken { get; init; }

    [Required]
    [MinLength(1)]
    public required string ApiSignature { get; init; }

    public long RequestDate { get; init; }
}
