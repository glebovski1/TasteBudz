namespace TasteBudz.Backend.Modules.Messaging;

public sealed record SupportThreadDto(
    Guid UserId,
    string Username,
    string DisplayName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastMessageAtUtc,
    string? LastMessagePreview,
    int MessageCount);
