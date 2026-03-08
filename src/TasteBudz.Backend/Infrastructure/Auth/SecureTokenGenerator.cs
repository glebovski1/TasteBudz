// Generates opaque URL-safe random tokens for access and refresh sessions.
using System.Security.Cryptography;

namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Produces high-entropy tokens without embedding user data or session metadata.
/// </summary>
public sealed class SecureTokenGenerator : ITokenGenerator
{
    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        // Convert to a URL-safe Base64 variant so tokens survive headers and logs cleanly.
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}