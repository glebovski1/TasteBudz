namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

internal sealed class UserAccountEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Domain.AccountStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
}

internal sealed class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Domain.UserRole Role { get; set; }
}

internal sealed class UserSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset RefreshExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

internal sealed class CuisineEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class ZipCoordinateEntity
{
    public string ZipCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

internal sealed class UserProfileEntity
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string HomeAreaZipCode { get; set; } = string.Empty;
    public Domain.SocialGoal? SocialGoal { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class UserPreferenceEntity
{
    public Guid UserId { get; set; }
    public Domain.SpiceTolerance? SpiceTolerance { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class UserCuisinePreferenceEntity
{
    public Guid UserId { get; set; }
    public Guid CuisineId { get; set; }
}

internal sealed class UserDietaryFlagEntity
{
    public Guid UserId { get; set; }
    public string DietaryFlag { get; set; } = string.Empty;
}

internal sealed class UserAllergyEntity
{
    public Guid UserId { get; set; }
    public string Allergy { get; set; } = string.Empty;
}

internal sealed class PrivacySettingEntity
{
    public Guid UserId { get; set; }
    public bool DiscoveryEnabled { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class RecurringAvailabilityWindowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Label { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class OneOffAvailabilityWindowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public string? Label { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class SwipeDecisionEntity
{
    public Guid ActorUserId { get; set; }
    public Guid SubjectUserId { get; set; }
    public Domain.SwipeDecisionType Decision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class BudConnectionEntity
{
    public Guid Id { get; set; }
    public Guid UserOneId { get; set; }
    public Guid UserTwoId { get; set; }
    public Domain.BudConnectionState State { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}

internal sealed class UserBlockEntity
{
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class GroupEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Domain.GroupVisibility Visibility { get; set; }
    public Domain.GroupLifecycleState LifecycleState { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class GroupMemberEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public Domain.GroupMemberState State { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class GroupInviteEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid InvitedUserId { get; set; }
    public Guid InviterUserId { get; set; }
    public Domain.GroupInviteStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class RestaurantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Domain.PriceTier PriceTier { get; set; }
    public string? ExternalPlaceId { get; set; }
}

internal sealed class RestaurantCuisineEntity
{
    public Guid RestaurantId { get; set; }
    public Guid CuisineId { get; set; }
}

internal sealed class RestaurantAdminAssignmentEntity
{
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

internal sealed class RestaurantSlotEntity
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public int Capacity { get; set; }
    public DateTimeOffset CutoffAtUtc { get; set; }
    public int? MinThresholdForDiscount { get; set; }
    public Domain.RestaurantSlotStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}

internal sealed class EventSlotReservationEntity
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid SlotId { get; set; }
    public Domain.EventSlotReservationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}

internal sealed class DiscountActivationEntity
{
    public Guid ReservationId { get; set; }
    public bool IsActive { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}

internal sealed class EventEntity
{
    public Guid Id { get; set; }
    public Guid HostUserId { get; set; }
    public string? Title { get; set; }
    public Domain.EventType EventType { get; set; }
    public Domain.EventStatus Status { get; set; }
    public DateTimeOffset EventStartAtUtc { get; set; }
    public DateTimeOffset DecisionAtUtc { get; set; }
    public int Capacity { get; set; }
    public int MinParticipantsToRun { get; set; }
    public Guid? SelectedRestaurantId { get; set; }
    public string? CuisineTarget { get; set; }
    public Guid? GroupId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

internal sealed class EventParticipantEntity
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Domain.EventParticipantState State { get; set; }
    public DateTimeOffset? InvitedAtUtc { get; set; }
    public DateTimeOffset? JoinedAtUtc { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
    public DateTimeOffset? RemovedAtUtc { get; set; }
}

internal sealed class ChatThreadEntity
{
    public Guid Id { get; set; }
    public Domain.ChatScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class ChatMessageEntity
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class MediaAssetEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? ProfileUserId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ReportId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public byte[] Content { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class NotificationEntity
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public Domain.NotificationType NotificationType { get; set; }
    public string ContextType { get; set; } = string.Empty;
    public Guid? ContextId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

internal sealed class ModerationReportEntity
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public Domain.ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public Guid? RelatedEventId { get; set; }
    public Guid? RelatedUserId { get; set; }
    public Guid? RelatedMessageId { get; set; }
    public Domain.ModerationReportStatus Status { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionDecision { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class ModerationActionEntity
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? ReportId { get; set; }
    public Domain.ModerationActionType ActionType { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class UserRestrictionEntity
{
    public Guid Id { get; set; }
    public Guid SubjectUserId { get; set; }
    public Guid IssuedByUserId { get; set; }
    public Guid? ModerationActionId { get; set; }
    public Domain.RestrictionScope Scope { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Domain.RestrictionStatus Status { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

internal sealed class AuditLogEntryEntity
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid? TargetEntityId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Details { get; set; } = string.Empty;
}
