using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LogoutRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public string? FullToken { get; init; }

    [Required]
    [MinLength(1)]
    public string? ApiSignature { get; init; }

    [Required]
    public long? RequestDate { get; init; }
}
