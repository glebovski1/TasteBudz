// Session token abstraction so auth logic is independent of the token-generation strategy.
namespace TasteBudz.Backend.Infrastructure.Auth;

/// <summary>
/// Produces opaque tokens for access and refresh sessions.
/// </summary>
public interface ITokenGenerator
{
    string GenerateToken();
}