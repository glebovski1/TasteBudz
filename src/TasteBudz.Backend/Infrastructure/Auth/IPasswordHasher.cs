// Password hashing abstraction so auth logic stays decoupled from the concrete algorithm.
namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Generates and verifies password hashes for user accounts.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Produces a versioned password hash suitable for storage.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a raw password against a previously stored hash.
    /// </summary>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}