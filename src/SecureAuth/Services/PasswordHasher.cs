using System.Globalization;
using System.Security.Cryptography;

namespace SecureAuth.Services;

public static class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";

    public static bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split(':');

        if (parts.Length != 4 ||
            !string.Equals(parts[0], Algorithm, StringComparison.Ordinal) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        if (iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);

            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            var actualHash = DeriveHash(password, salt, iterations, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static byte[] DeriveHash(string password, byte[] salt, int iterations, int hashSize)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashSize);
    }
}
