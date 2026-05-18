using System.ComponentModel.DataAnnotations;

namespace SecureAuth.Contracts;

public sealed class LogoutRequest : ISignedRequest
{
    [Required]
    [MinLength(1)]
    public string FullToken { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ApiSignature { get; init; } = string.Empty;

    public long RequestDate { get; init; }
}
