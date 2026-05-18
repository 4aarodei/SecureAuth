using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SecureAuth.Config;
using SecureAuth.Services;
using Xunit;

namespace SecureAuth.Tests;

public sealed class ApiSignatureValidatorTests
{
    [Fact]
    public void Validate_ReturnsSuccess_ForValidSignatureAndFreshTimestamp()
    {
        var options = Options.Create(new SecurityOptions
        {
            StaticKey = "test-static-key",
            RequestFreshnessMinutes = 5
        });
        var validator = new ApiSignatureValidator(options);
        var requestDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var signature = BuildSignature(options.Value.StaticKey, requestDate);
        var result = validator.Validate(signature, requestDate);

        Assert.Equal(ApiSignatureValidationResult.Success, result);
    }

    [Fact]
    public void Validate_ReturnsInvalidSignature_ForWrongLength()
    {
        var validator = new ApiSignatureValidator(Options.Create(new SecurityOptions
        {
            StaticKey = "test-static-key",
            RequestFreshnessMinutes = 5
        }));

        var result = validator.Validate("1234", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Assert.Equal(ApiSignatureValidationResult.InvalidSignature, result);
    }

    private static string BuildSignature(string staticKey, long requestDate)
    {
        var payload = staticKey + requestDate.ToString(CultureInfo.InvariantCulture);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
