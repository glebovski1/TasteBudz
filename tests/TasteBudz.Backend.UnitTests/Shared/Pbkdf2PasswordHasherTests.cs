// Unit tests for the password hasher used by the auth flow.
using TasteBudz.Backend.Infrastructure.Auth;

namespace TasteBudz.Backend.UnitTests.Shared;

/// <summary>
/// Verifies that hashed passwords are not stored in plain text and can be validated correctly.
/// </summary>
public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void HashPassword_ProducesVerifiableHash()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hashedPassword = hasher.HashPassword("Pa$$w0rd123");

        Assert.NotEqual("Pa$$w0rd123", hashedPassword);
        Assert.True(hasher.VerifyPassword(hashedPassword, "Pa$$w0rd123"));
        Assert.False(hasher.VerifyPassword(hashedPassword, "wrong-password"));
    }
}