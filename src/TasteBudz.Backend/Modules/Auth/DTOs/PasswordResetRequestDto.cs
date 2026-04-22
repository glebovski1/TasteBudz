namespace TasteBudz.Backend.Modules.Auth;

public sealed record PasswordResetRequestDto(
    Guid RequestId,
    string Username,
    string Message,
    Guid? MatchedUserId,
    string? MatchedUsername,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedByUserId);
