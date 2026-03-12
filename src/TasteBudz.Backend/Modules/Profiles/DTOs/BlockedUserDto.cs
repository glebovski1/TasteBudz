namespace TasteBudz.Backend.Modules.Profiles;

public sealed record BlockedUserDto(
    Guid UserId,
    string Username,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);
