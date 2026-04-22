using Microsoft.EntityFrameworkCore;

namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// EF Core model for the relational MVP runtime, mapped to the canonical SQL schema.
/// </summary>
public sealed class TasteBudzDbContext(DbContextOptions<TasteBudzDbContext> options) : DbContext(options)
{
    internal DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
    internal DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    internal DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();
    internal DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    internal DbSet<PasswordResetRequestEntity> PasswordResetRequests => Set<PasswordResetRequestEntity>();
    internal DbSet<CuisineEntity> Cuisines => Set<CuisineEntity>();
    internal DbSet<ZipCoordinateEntity> ZipCoordinates => Set<ZipCoordinateEntity>();
    internal DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    internal DbSet<UserPreferenceEntity> UserPreferences => Set<UserPreferenceEntity>();
    internal DbSet<UserCuisinePreferenceEntity> UserCuisinePreferences => Set<UserCuisinePreferenceEntity>();
    internal DbSet<UserDietaryFlagEntity> UserDietaryFlags => Set<UserDietaryFlagEntity>();
    internal DbSet<UserAllergyEntity> UserAllergies => Set<UserAllergyEntity>();
    internal DbSet<PrivacySettingEntity> PrivacySettings => Set<PrivacySettingEntity>();
    internal DbSet<RecurringAvailabilityWindowEntity> RecurringAvailabilityWindows => Set<RecurringAvailabilityWindowEntity>();
    internal DbSet<OneOffAvailabilityWindowEntity> OneOffAvailabilityWindows => Set<OneOffAvailabilityWindowEntity>();
    internal DbSet<SwipeDecisionEntity> SwipeDecisions => Set<SwipeDecisionEntity>();
    internal DbSet<BudConnectionEntity> BudConnections => Set<BudConnectionEntity>();
    internal DbSet<UserBlockEntity> UserBlocks => Set<UserBlockEntity>();
    internal DbSet<GroupEntity> Groups => Set<GroupEntity>();
    internal DbSet<GroupMemberEntity> GroupMembers => Set<GroupMemberEntity>();
    internal DbSet<GroupInviteEntity> GroupInvites => Set<GroupInviteEntity>();
    internal DbSet<RestaurantEntity> Restaurants => Set<RestaurantEntity>();
    internal DbSet<RestaurantCuisineEntity> RestaurantCuisines => Set<RestaurantCuisineEntity>();
    internal DbSet<RestaurantAdminAssignmentEntity> RestaurantAdminAssignments => Set<RestaurantAdminAssignmentEntity>();
    internal DbSet<RestaurantSlotEntity> RestaurantSlots => Set<RestaurantSlotEntity>();
    internal DbSet<EventSlotReservationEntity> EventSlotReservations => Set<EventSlotReservationEntity>();
    internal DbSet<DiscountActivationEntity> DiscountActivations => Set<DiscountActivationEntity>();
    internal DbSet<CheckoutSessionEntity> CheckoutSessions => Set<CheckoutSessionEntity>();
    internal DbSet<EventEntity> Events => Set<EventEntity>();
    internal DbSet<EventParticipantEntity> EventParticipants => Set<EventParticipantEntity>();
    internal DbSet<EventFeedbackEntity> EventFeedbacks => Set<EventFeedbackEntity>();
    internal DbSet<EventFeedbackPhotoEntity> EventFeedbackPhotos => Set<EventFeedbackPhotoEntity>();
    internal DbSet<ChatThreadEntity> ChatThreads => Set<ChatThreadEntity>();
    internal DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();
    internal DbSet<MediaAssetEntity> MediaAssets => Set<MediaAssetEntity>();
    internal DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    internal DbSet<ModerationReportEntity> ModerationReports => Set<ModerationReportEntity>();
    internal DbSet<ModerationActionEntity> ModerationActions => Set<ModerationActionEntity>();
    internal DbSet<UserRestrictionEntity> UserRestrictions => Set<UserRestrictionEntity>();
    internal DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccountEntity>(entity =>
        {
            entity.ToTable("UserAccounts");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.NormalizedUsername).IsUnique();
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(item => new { item.UserId, item.Role });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<UserSessionEntity>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.AccessToken).IsUnique();
            entity.HasIndex(item => item.RefreshToken).IsUnique();
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<PasswordResetTokenEntity>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.CreatedAtUtc });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.CreatedByUserId);
        });

        modelBuilder.Entity<PasswordResetRequestEntity>(entity =>
        {
            entity.ToTable("PasswordResetRequests");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ClosedAtUtc, item.CreatedAtUtc });
            entity.HasIndex(item => item.MatchedUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.MatchedUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ClosedByUserId);
        });

        modelBuilder.Entity<CuisineEntity>(entity =>
        {
            entity.ToTable("Cuisines");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<ZipCoordinateEntity>(entity =>
        {
            entity.ToTable("ZipCoordinates");
            entity.HasKey(item => item.ZipCode);
        });

        modelBuilder.Entity<UserProfileEntity>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(item => item.UserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<UserPreferenceEntity>(entity =>
        {
            entity.ToTable("UserPreferences");
            entity.HasKey(item => item.UserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<UserCuisinePreferenceEntity>(entity =>
        {
            entity.ToTable("UserCuisinePreferences");
            entity.HasKey(item => new { item.UserId, item.CuisineId });
            entity.HasOne<UserPreferenceEntity>().WithMany().HasForeignKey(item => item.UserId);
            entity.HasOne<CuisineEntity>().WithMany().HasForeignKey(item => item.CuisineId);
        });

        modelBuilder.Entity<UserDietaryFlagEntity>(entity =>
        {
            entity.ToTable("UserDietaryFlags");
            entity.HasKey(item => new { item.UserId, item.DietaryFlag });
            entity.HasOne<UserPreferenceEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<UserAllergyEntity>(entity =>
        {
            entity.ToTable("UserAllergies");
            entity.HasKey(item => new { item.UserId, item.Allergy });
            entity.HasOne<UserPreferenceEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<PrivacySettingEntity>(entity =>
        {
            entity.ToTable("PrivacySettings");
            entity.HasKey(item => item.UserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<RecurringAvailabilityWindowEntity>(entity =>
        {
            entity.ToTable("RecurringAvailabilityWindows");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<OneOffAvailabilityWindowEntity>(entity =>
        {
            entity.ToTable("OneOffAvailabilityWindows");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<SwipeDecisionEntity>(entity =>
        {
            entity.ToTable("SwipeDecisions");
            entity.HasKey(item => new { item.ActorUserId, item.SubjectUserId });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ActorUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.SubjectUserId);
        });

        modelBuilder.Entity<BudConnectionEntity>(entity =>
        {
            entity.ToTable("BudConnections");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserOneId, item.UserTwoId }).IsUnique();
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserOneId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserTwoId);
        });

        modelBuilder.Entity<UserBlockEntity>(entity =>
        {
            entity.ToTable("UserBlocks");
            entity.HasKey(item => new { item.BlockerUserId, item.BlockedUserId });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.BlockerUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.BlockedUserId);
        });

        modelBuilder.Entity<GroupEntity>(entity =>
        {
            entity.ToTable("Groups");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.OwnerUserId);
        });

        modelBuilder.Entity<GroupMemberEntity>(entity =>
        {
            entity.ToTable("GroupMembers");
            entity.HasKey(item => new { item.GroupId, item.UserId });
            entity.HasIndex(item => item.UserId);
            entity.HasOne<GroupEntity>().WithMany().HasForeignKey(item => item.GroupId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<GroupInviteEntity>(entity =>
        {
            entity.ToTable("GroupInvites");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.InvitedUserId);
            entity.HasOne<GroupEntity>().WithMany().HasForeignKey(item => item.GroupId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.InvitedUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.InviterUserId);
        });

        modelBuilder.Entity<RestaurantEntity>(entity =>
        {
            entity.ToTable("Restaurants");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ZipCode);
        });

        modelBuilder.Entity<RestaurantCuisineEntity>(entity =>
        {
            entity.ToTable("RestaurantCuisines");
            entity.HasKey(item => new { item.RestaurantId, item.CuisineId });
            entity.HasOne<RestaurantEntity>().WithMany().HasForeignKey(item => item.RestaurantId);
            entity.HasOne<CuisineEntity>().WithMany().HasForeignKey(item => item.CuisineId);
        });

        modelBuilder.Entity<RestaurantAdminAssignmentEntity>(entity =>
        {
            entity.ToTable("RestaurantAdminAssignments");
            entity.HasKey(item => new { item.RestaurantId, item.UserId });
            entity.HasIndex(item => item.UserId);
            entity.HasOne<RestaurantEntity>().WithMany().HasForeignKey(item => item.RestaurantId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<RestaurantSlotEntity>(entity =>
        {
            entity.ToTable("RestaurantSlots");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.RestaurantId, item.StartsAtUtc });
            entity.HasOne<RestaurantEntity>().WithMany().HasForeignKey(item => item.RestaurantId);
        });

        modelBuilder.Entity<EventSlotReservationEntity>(entity =>
        {
            entity.ToTable("EventSlotReservations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.EventId).HasFilter("Status = 0").IsUnique();
            entity.HasIndex(item => item.SlotId).HasFilter("Status = 0").IsUnique();
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.EventId);
            entity.HasOne<RestaurantSlotEntity>().WithMany().HasForeignKey(item => item.SlotId);
        });

        modelBuilder.Entity<DiscountActivationEntity>(entity =>
        {
            entity.ToTable("DiscountActivations");
            entity.HasKey(item => item.ReservationId);
            entity.HasOne<EventSlotReservationEntity>().WithMany().HasForeignKey(item => item.ReservationId);
        });

        modelBuilder.Entity<CheckoutSessionEntity>(entity =>
        {
            entity.ToTable("CheckoutSessions");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.EventId, item.UserId, item.CreatedAtUtc });
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.EventId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.GroupId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.HostUserId);
            entity.HasOne<RestaurantEntity>().WithMany().HasForeignKey(item => item.SelectedRestaurantId);
            entity.HasOne<GroupEntity>().WithMany().HasForeignKey(item => item.GroupId);
        });

        modelBuilder.Entity<EventParticipantEntity>(entity =>
        {
            entity.ToTable("EventParticipants");
            entity.HasKey(item => new { item.EventId, item.UserId });
            entity.HasIndex(item => item.UserId);
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.EventId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<EventFeedbackEntity>(entity =>
        {
            entity.ToTable("EventFeedbacks");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.EventId, item.AuthorUserId }).IsUnique();
            entity.HasIndex(item => new { item.EventId, item.CreatedAtUtc });
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.EventId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.AuthorUserId);
        });

        modelBuilder.Entity<EventFeedbackPhotoEntity>(entity =>
        {
            entity.ToTable("EventFeedbackPhotos");
            entity.HasKey(item => new { item.EventFeedbackId, item.MediaAssetId });
            entity.HasIndex(item => item.MediaAssetId).IsUnique();
            entity.HasOne<EventFeedbackEntity>().WithMany().HasForeignKey(item => item.EventFeedbackId);
            entity.HasOne<MediaAssetEntity>().WithMany().HasForeignKey(item => item.MediaAssetId);
        });

        modelBuilder.Entity<ChatThreadEntity>(entity =>
        {
            entity.ToTable("ChatThreads");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ScopeType, item.ScopeId }).IsUnique();
        });

        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ThreadId, item.CreatedAtUtc, item.Id });
            entity.HasOne<ChatThreadEntity>().WithMany().HasForeignKey(item => item.ThreadId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.SenderUserId);
        });

        modelBuilder.Entity<MediaAssetEntity>(entity =>
        {
            entity.ToTable("MediaAssets");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ProfileUserId);
            entity.HasIndex(item => item.EventId);
            entity.HasIndex(item => item.ReportId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.OwnerUserId);
            entity.HasOne<UserProfileEntity>().WithMany().HasForeignKey(item => item.ProfileUserId);
            entity.HasOne<GroupEntity>().WithMany().HasForeignKey(item => item.GroupId);
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.EventId);
            entity.HasOne<ModerationReportEntity>().WithMany().HasForeignKey(item => item.ReportId);
        });

        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.RecipientUserId, item.CreatedAtUtc });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.RecipientUserId);
        });

        modelBuilder.Entity<ModerationReportEntity>(entity =>
        {
            entity.ToTable("ModerationReports");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Status, item.CreatedAtUtc });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ReporterUserId);
            entity.HasOne<EventEntity>().WithMany().HasForeignKey(item => item.RelatedEventId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.RelatedUserId);
            entity.HasOne<ChatMessageEntity>().WithMany().HasForeignKey(item => item.RelatedMessageId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ResolvedByUserId);
        });

        modelBuilder.Entity<ModerationActionEntity>(entity =>
        {
            entity.ToTable("ModerationActions");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ActorUserId);
            entity.HasOne<ModerationReportEntity>().WithMany().HasForeignKey(item => item.ReportId);
        });

        modelBuilder.Entity<UserRestrictionEntity>(entity =>
        {
            entity.ToTable("UserRestrictions");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.SubjectUserId, item.Scope, item.Status });
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.SubjectUserId);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.IssuedByUserId);
            entity.HasOne<ModerationActionEntity>().WithMany().HasForeignKey(item => item.ModerationActionId);
        });

        modelBuilder.Entity<AuditLogEntryEntity>(entity =>
        {
            entity.ToTable("AuditLogEntries");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserAccountEntity>().WithMany().HasForeignKey(item => item.ActorUserId);
        });
    }
}
