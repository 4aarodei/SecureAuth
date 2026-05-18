using System.Security.Cryptography;

namespace SecureAuth.Services;

public static class TokenGenerator
{
    private const int TokenSizeBytes = 32;

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
