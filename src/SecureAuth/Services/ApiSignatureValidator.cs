using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SecureAuth.Config;

namespace SecureAuth.Services;

public sealed class ApiSignatureValidator
{
    private const int Sha256HexLength = 64;
    private readonly SecurityOptions _options;

    public ApiSignatureValidator(IOptions<SecurityOptions> options)
    {
        _options = options.Value;
    }

    public ApiSignatureValidationResult Validate(string? apiSignature, long? requestDate)
    {
        if (!IsValidSignature(apiSignature, requestDate))
        {
            return ApiSignatureValidationResult.InvalidSignature;
        }

        if (!IsFresh(requestDate))
        {
            return ApiSignatureValidationResult.StaleRequest;
        }

        return ApiSignatureValidationResult.Success;
    }

    private bool IsValidSignature(string? apiSignature, long? requestDate)
    {
        if (requestDate is null || string.IsNullOrWhiteSpace(apiSignature) || apiSignature.Length != Sha256HexLength)
        {
            return false;
        }

        byte[] providedHash;

        try
        {
            providedHash = Convert.FromHexString(apiSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var payload = _options.StaticKey + requestDate.Value.ToString(CultureInfo.InvariantCulture);
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }

    private bool IsFresh(long? requestDate)
    {
        if (requestDate is null)
        {
            return false;
        }

        try
        {
            var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(requestDate.Value);
            var allowedDifference = TimeSpan.FromMinutes(_options.RequestFreshnessMinutes);
            var difference = (requestTime - DateTimeOffset.UtcNow).Duration();

            return difference <= allowedDifference;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
