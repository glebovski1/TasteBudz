namespace TasteBudz.Backend.Modules.Auth;

public sealed record PasswordResetTokenDto(
    Guid UserId,
    string Username,
    string ResetToken,
    string ResetUrl,
    DateTimeOffset ExpiresAtUtc);
