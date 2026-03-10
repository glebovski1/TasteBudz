// PBKDF2-based password hasher used by the MVP's in-memory auth flow.
using System.Security.Cryptography;

namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Stores password hashes in a simple versioned format:
/// <c>version.iterations.base64Salt.base64Hash</c>.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // The version prefix makes later hash migrations possible without breaking old accounts.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var parts = hashedPassword.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4 || !string.Equals(parts[0], "v1", StringComparison.Ordinal) || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Compare the raw bytes in constant time to avoid leaking information via timing differences.
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}