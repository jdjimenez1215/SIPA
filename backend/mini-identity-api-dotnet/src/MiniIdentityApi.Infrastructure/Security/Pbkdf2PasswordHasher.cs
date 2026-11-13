using System.Security.Cryptography;
using System.Text;
using MiniIdentityApi.Application.Interfaces;

namespace MiniIdentityApi.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 key derivation (OWASP 2023 recommendation for PBKDF2).
/// The iteration count is stored inside the hash so it can be raised later without breaking old hashes:
/// <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;base64(hash)&gt;</c>.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int DefaultIterations = 210_000;

    public string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string plainText, string salt)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var saltBytes = DecodeSalt(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText),
            saltBytes,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{Prefix}${DefaultIterations}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plainText, string salt, string expectedHash)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(expectedHash))
            return false;

        var parts = expectedHash.Split('$', 3);
        if (parts.Length != 3 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        var expected = new byte[KeySize];
        if (!Convert.TryFromBase64String(parts[2], expected, out var written))
            return false;
        if (written != KeySize)
            return false;

        byte[] saltBytes;
        try
        {
            saltBytes = DecodeSalt(salt);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText),
            saltBytes,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        // Constant-time comparison: never leak how many bytes matched.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] DecodeSalt(string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        if (saltBytes.Length < SaltSize)
            throw new FormatException("Salt too short.");
        return saltBytes;
    }
}
