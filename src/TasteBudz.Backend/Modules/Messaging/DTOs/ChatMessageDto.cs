namespace TasteBudz.Backend.Modules.Messaging;

public sealed record ChatMessageDto(
    Guid MessageId,
    Guid SenderUserId,
    string SenderUsername,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAtUtc);
