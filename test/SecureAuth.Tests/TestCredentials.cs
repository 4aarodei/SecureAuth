using System.Security.Cryptography;

namespace SecureAuth.Tests;

internal static class TestCredentials
{
    public static readonly string Password = CreatePassword();

    public static readonly string PasswordHash = CreatePasswordHash(Password);

    private static string CreatePassword()
    {
        return "test-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }

    private static string CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"pbkdf2-sha256:100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
