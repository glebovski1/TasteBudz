namespace TasteBudz.Backend.Modules.Messaging;

public sealed record DirectChatDto(
    Guid DirectChatId,
    Guid OtherUserId,
    string OtherUsername,
    string OtherDisplayName,
    DateTimeOffset CreatedAtUtc);
