USE dbTasteBudz;
GO

-----------------------------------------------------------------------
-- 1. AUTHENTICATION, IDENTITY & ACCESS
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAccounts (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        NormalizedUsername NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        NormalizedEmail NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        DeletedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT UQ_UserAccounts_Username UNIQUE (Username),
        CONSTRAINT UQ_UserAccounts_NormalizedUsername UNIQUE (NormalizedUsername),
        CONSTRAINT UQ_UserAccounts_Email UNIQUE (Email),
        CONSTRAINT UQ_UserAccounts_NormalizedEmail UNIQUE (NormalizedEmail)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserId UNIQUEIDENTIFIER NOT NULL,
        Role INT NOT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, Role),
        CONSTRAINT FK_UserRoles_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserSessions PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        AccessToken NVARCHAR(MAX) NOT NULL,
        RefreshToken NVARCHAR(MAX) NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET NOT NULL,
        RefreshExpiresAtUtc DATETIMEOFFSET NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_UserSessions_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_UserSessions_RefreshAfterAccess CHECK (RefreshExpiresAtUtc > ExpiresAtUtc),
        CONSTRAINT CK_UserSessions_RevokedAfterCreate CHECK (RevokedAtUtc IS NULL OR RevokedAtUtc >= CreatedAtUtc)
    );
END
GO

-----------------------------------------------------------------------
-- 2. SHARED REFERENCE DATA
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Cuisines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cuisines (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Cuisines PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL,
        CONSTRAINT UQ_Cuisines_Name UNIQUE (Name)
    );
END
GO

-----------------------------------------------------------------------
-- 3. PROFILES, PREFERENCES & PRIVACY
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.UserProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserProfiles (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserProfiles PRIMARY KEY,
        DisplayName NVARCHAR(100) NOT NULL,
        Bio NVARCHAR(MAX) NULL,
        HomeAreaZipCode NVARCHAR(20) NOT NULL,
        SocialGoal INT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_UserProfiles_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPreferences (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY,
        SpiceTolerance INT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_UserPreferences_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserCuisinePreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCuisinePreferences (
        UserId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UserCuisinePreferences PRIMARY KEY (UserId, CuisineId),
        CONSTRAINT FK_UserCuisinePreferences_UserPreferences FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId),
        CONSTRAINT FK_UserCuisinePreferences_Cuisines FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserDietaryFlags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDietaryFlags (
        UserId UNIQUEIDENTIFIER NOT NULL,
        DietaryFlag NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_UserDietaryFlags PRIMARY KEY (UserId, DietaryFlag),
        CONSTRAINT FK_UserDietaryFlags_UserPreferences FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId),
        CONSTRAINT CK_UserDietaryFlags_NonBlank CHECK (LEN(LTRIM(RTRIM(DietaryFlag))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.UserAllergies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAllergies (
        UserId UNIQUEIDENTIFIER NOT NULL,
        Allergy NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_UserAllergies PRIMARY KEY (UserId, Allergy),
        CONSTRAINT FK_UserAllergies_UserPreferences FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId),
        CONSTRAINT CK_UserAllergies_NonBlank CHECK (LEN(LTRIM(RTRIM(Allergy))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.PrivacySettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivacySettings (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PrivacySettings PRIMARY KEY,
        DiscoveryEnabled BIT NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_PrivacySettings_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

-----------------------------------------------------------------------
-- 4. AVAILABILITY (RECURRING & ONE-OFF)
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.RecurringAvailabilityWindows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecurringAvailabilityWindows (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RecurringAvailabilityWindows PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        DayOfWeek INT NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        Label NVARCHAR(100) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_RecurringAvailabilityWindows_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_RecurringAvailabilityWindows_DayOfWeek CHECK (DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_RecurringAvailabilityWindows_TimeRange CHECK (StartTime < EndTime)
    );
END
GO

IF OBJECT_ID(N'dbo.OneOffAvailabilityWindows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OneOffAvailabilityWindows (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OneOffAvailabilityWindows PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        StartsAtUtc DATETIMEOFFSET NOT NULL,
        EndsAtUtc DATETIMEOFFSET NOT NULL,
        Label NVARCHAR(100) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_OneOffAvailabilityWindows_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_OneOffAvailabilityWindows_TimeRange CHECK (StartsAtUtc < EndsAtUtc)
    );
END
GO

-----------------------------------------------------------------------
-- 5. SOCIAL DISCOVERY, CONNECTIONS & SAFETY
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.SwipeDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SwipeDecisions (
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        SubjectUserId UNIQUEIDENTIFIER NOT NULL,
        Decision INT NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT PK_SwipeDecisions PRIMARY KEY (ActorUserId, SubjectUserId),
        CONSTRAINT FK_SwipeDecisions_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_SwipeDecisions_Subject FOREIGN KEY (SubjectUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_SwipeDecisions_NoSelfSwipe CHECK (ActorUserId <> SubjectUserId)
    );
END
GO

IF OBJECT_ID(N'dbo.BudConnections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BudConnections (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BudConnections PRIMARY KEY,
        UserOneId UNIQUEIDENTIFIER NOT NULL,
        UserTwoId UNIQUEIDENTIFIER NOT NULL,
        State INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        EndedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_BudConnections_UserOne FOREIGN KEY (UserOneId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_BudConnections_UserTwo FOREIGN KEY (UserTwoId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT UQ_BudConnections_UserPair UNIQUE (UserOneId, UserTwoId),
        CONSTRAINT CK_BudConnections_NoSelfPair CHECK (UserOneId <> UserTwoId),
        CONSTRAINT CK_BudConnections_NormalizedPair CHECK (UserOneId < UserTwoId),
        CONSTRAINT CK_BudConnections_EndedAfterCreate CHECK (EndedAtUtc IS NULL OR EndedAtUtc >= CreatedAtUtc)
    );
END
GO

IF OBJECT_ID(N'dbo.UserBlocks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserBlocks (
        BlockerUserId UNIQUEIDENTIFIER NOT NULL,
        BlockedUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT PK_UserBlocks PRIMARY KEY (BlockerUserId, BlockedUserId),
        CONSTRAINT FK_UserBlocks_Blocker FOREIGN KEY (BlockerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserBlocks_Blocked FOREIGN KEY (BlockedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_UserBlocks_NoSelfBlock CHECK (BlockerUserId <> BlockedUserId)
    );
END
GO

-----------------------------------------------------------------------
-- 6. GROUPS, MEMBERSHIP & INVITES
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Groups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Groups (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Groups PRIMARY KEY,
        OwnerUserId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Visibility INT NOT NULL,
        LifecycleState INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_Groups_OwnerUser FOREIGN KEY (OwnerUserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.GroupMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GroupMembers (
        GroupId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        State INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT PK_GroupMembers PRIMARY KEY (GroupId, UserId),
        CONSTRAINT FK_GroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_GroupMembers_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.GroupInvites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GroupInvites (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_GroupInvites PRIMARY KEY,
        GroupId UNIQUEIDENTIFIER NOT NULL,
        InvitedUserId UNIQUEIDENTIFIER NOT NULL,
        InviterUserId UNIQUEIDENTIFIER NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_GroupInvites_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_GroupInvites_InvitedUser FOREIGN KEY (InvitedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_GroupInvites_InviterUser FOREIGN KEY (InviterUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_GroupInvites_NoSelfInvite CHECK (InvitedUserId <> InviterUserId)
    );
END
GO

-----------------------------------------------------------------------
-- 7. RESTAURANTS & EVENTS
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Restaurants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Restaurants (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Restaurants PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        State NVARCHAR(50) NOT NULL,
        ZipCode NVARCHAR(20) NOT NULL,
        Latitude FLOAT NULL,
        Longitude FLOAT NULL,
        PriceTier INT NOT NULL,
        ExternalPlaceId NVARCHAR(255) NULL,
        CONSTRAINT CK_Restaurants_Latitude CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
        CONSTRAINT CK_Restaurants_Longitude CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
        CONSTRAINT CK_Restaurants_CoordinatesTogether CHECK (
            (Latitude IS NULL AND Longitude IS NULL) OR
            (Latitude IS NOT NULL AND Longitude IS NOT NULL)
        )
    );
END
GO

IF OBJECT_ID(N'dbo.RestaurantCuisines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantCuisines (
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_RestaurantCuisines PRIMARY KEY (RestaurantId, CuisineId),
        CONSTRAINT FK_RestaurantCuisines_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_RestaurantCuisines_Cuisines FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
        HostUserId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NULL,
        EventType INT NOT NULL,
        Status INT NOT NULL,
        EventStartAtUtc DATETIMEOFFSET NOT NULL,
        DecisionAtUtc DATETIMEOFFSET NOT NULL,
        Capacity INT NOT NULL,
        MinParticipantsToRun INT NOT NULL CONSTRAINT DF_Events_MinParticipantsToRun DEFAULT (2),
        SelectedRestaurantId UNIQUEIDENTIFIER NULL,
        CuisineTarget NVARCHAR(100) NULL,
        GroupId UNIQUEIDENTIFIER NULL,
        CancellationReason NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CancelledAtUtc DATETIMEOFFSET NULL,
        CompletedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_Events_HostUser FOREIGN KEY (HostUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_Events_SelectedRestaurant FOREIGN KEY (SelectedRestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_Events_Group FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT CK_Events_CapacityRange CHECK (Capacity BETWEEN 2 AND 8),
        CONSTRAINT CK_Events_MinParticipantsRange CHECK (MinParticipantsToRun BETWEEN 2 AND Capacity),
        CONSTRAINT CK_Events_DecisionBeforeStart CHECK (DecisionAtUtc < EventStartAtUtc),
        CONSTRAINT CK_Events_SelectedRestaurantOrCuisineTarget CHECK (
            (CASE WHEN SelectedRestaurantId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN NULLIF(LTRIM(RTRIM(CuisineTarget)), N'') IS NULL THEN 0 ELSE 1 END) = 1
        ),
        CONSTRAINT CK_Events_CancelledAfterCreate CHECK (CancelledAtUtc IS NULL OR CancelledAtUtc >= CreatedAtUtc),
        CONSTRAINT CK_Events_CompletedAfterStart CHECK (CompletedAtUtc IS NULL OR CompletedAtUtc >= EventStartAtUtc)
    );
END
GO

IF OBJECT_ID(N'dbo.EventParticipants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventParticipants (
        EventId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        State INT NOT NULL,
        InvitedAtUtc DATETIMEOFFSET NULL,
        JoinedAtUtc DATETIMEOFFSET NULL,
        RespondedAtUtc DATETIMEOFFSET NULL,
        LeftAtUtc DATETIMEOFFSET NULL,
        RemovedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT PK_EventParticipants PRIMARY KEY (EventId, UserId),
        CONSTRAINT FK_EventParticipants_Events FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_EventParticipants_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END
GO

-----------------------------------------------------------------------
-- 8. CHAT & NOTIFICATIONS
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatThreads (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChatThreads PRIMARY KEY,
        ScopeType INT NOT NULL,
        ScopeId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT UQ_ChatThreads_Scope UNIQUE (ScopeType, ScopeId)
    );
END
GO

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ThreadId UNIQUEIDENTIFIER NOT NULL,
        SenderUserId UNIQUEIDENTIFIER NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_ChatMessages_ChatThreads FOREIGN KEY (ThreadId) REFERENCES dbo.ChatThreads (Id),
        CONSTRAINT FK_ChatMessages_SenderUser FOREIGN KEY (SenderUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_ChatMessages_NonBlankBody CHECK (LEN(LTRIM(RTRIM(Body))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
        RecipientUserId UNIQUEIDENTIFIER NOT NULL,
        NotificationType INT NOT NULL,
        ContextType NVARCHAR(100) NOT NULL,
        ContextId UNIQUEIDENTIFIER NULL,
        Message NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        ReadAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_Notifications_RecipientUser FOREIGN KEY (RecipientUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_Notifications_NonBlankContextType CHECK (LEN(LTRIM(RTRIM(ContextType))) > 0),
        CONSTRAINT CK_Notifications_NonBlankMessage CHECK (LEN(LTRIM(RTRIM(Message))) > 0),
        CONSTRAINT CK_Notifications_ReadAfterCreate CHECK (ReadAtUtc IS NULL OR ReadAtUtc >= CreatedAtUtc)
    );
END
GO

-----------------------------------------------------------------------
-- 9. MODERATION, RESTRICTIONS & AUDIT
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ModerationReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationReports (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ModerationReports PRIMARY KEY,
        ReporterUserId UNIQUEIDENTIFIER NOT NULL,
        TargetType INT NOT NULL,
        TargetId UNIQUEIDENTIFIER NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Reason NVARCHAR(MAX) NOT NULL,
        Explanation NVARCHAR(MAX) NULL,
        RelatedEventId UNIQUEIDENTIFIER NULL,
        RelatedUserId UNIQUEIDENTIFIER NULL,
        RelatedMessageId UNIQUEIDENTIFIER NULL,
        Status INT NOT NULL,
        ResolvedByUserId UNIQUEIDENTIFIER NULL,
        ResolvedAtUtc DATETIMEOFFSET NULL,
        ResolutionDecision NVARCHAR(MAX) NULL,
        ResolutionNotes NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_ModerationReports_ReporterUser FOREIGN KEY (ReporterUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationReports_RelatedEvent FOREIGN KEY (RelatedEventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_ModerationReports_RelatedUser FOREIGN KEY (RelatedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationReports_RelatedMessage FOREIGN KEY (RelatedMessageId) REFERENCES dbo.ChatMessages (Id),
        CONSTRAINT FK_ModerationReports_ResolvedByUser FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_ModerationReports_NonBlankCategory CHECK (LEN(LTRIM(RTRIM(Category))) > 0),
        CONSTRAINT CK_ModerationReports_NonBlankReason CHECK (LEN(LTRIM(RTRIM(Reason))) > 0),
        CONSTRAINT CK_ModerationReports_ResolvedAfterCreate CHECK (ResolvedAtUtc IS NULL OR ResolvedAtUtc >= CreatedAtUtc)
    );
END
GO

IF OBJECT_ID(N'dbo.ModerationActions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationActions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ModerationActions PRIMARY KEY,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        ReportId UNIQUEIDENTIFIER NULL,
        ActionType INT NOT NULL,
        Notes NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_ModerationActions_ActorUser FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationActions_Report FOREIGN KEY (ReportId) REFERENCES dbo.ModerationReports (Id),
        CONSTRAINT CK_ModerationActions_NonBlankNotes CHECK (LEN(LTRIM(RTRIM(Notes))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRestrictions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRestrictions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserRestrictions PRIMARY KEY,
        SubjectUserId UNIQUEIDENTIFIER NOT NULL,
        IssuedByUserId UNIQUEIDENTIFIER NOT NULL,
        ModerationActionId UNIQUEIDENTIFIER NULL,
        Scope INT NOT NULL,
        Reason NVARCHAR(MAX) NOT NULL,
        StartsAtUtc DATETIMEOFFSET NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET NULL,
        Status INT NOT NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_UserRestrictions_SubjectUser FOREIGN KEY (SubjectUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserRestrictions_IssuedByUser FOREIGN KEY (IssuedByUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserRestrictions_ModerationAction FOREIGN KEY (ModerationActionId) REFERENCES dbo.ModerationActions (Id),
        CONSTRAINT CK_UserRestrictions_NonBlankReason CHECK (LEN(LTRIM(RTRIM(Reason))) > 0),
        CONSTRAINT CK_UserRestrictions_ExpiresAfterStart CHECK (ExpiresAtUtc IS NULL OR ExpiresAtUtc > StartsAtUtc),
        CONSTRAINT CK_UserRestrictions_RevokedAfterStart CHECK (RevokedAtUtc IS NULL OR RevokedAtUtc >= StartsAtUtc)
    );
END
GO

IF OBJECT_ID(N'dbo.AuditLogEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogEntries (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditLogEntries PRIMARY KEY,
        ActionType NVARCHAR(100) NOT NULL,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        TargetEntityType NVARCHAR(100) NOT NULL,
        TargetEntityId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        Details NVARCHAR(MAX) NOT NULL,
        CONSTRAINT FK_AuditLogEntries_ActorUser FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_AuditLogEntries_NonBlankActionType CHECK (LEN(LTRIM(RTRIM(ActionType))) > 0),
        CONSTRAINT CK_AuditLogEntries_NonBlankTargetEntityType CHECK (LEN(LTRIM(RTRIM(TargetEntityType))) > 0),
        CONSTRAINT CK_AuditLogEntries_NonBlankDetails CHECK (LEN(LTRIM(RTRIM(Details))) > 0)
    );
END
GO

-----------------------------------------------------------------------
-- 10. FUTURE EXTENSION TABLES (NOT USED BY CURRENT MVP FLOWS)
-----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.RestaurantReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantReviews (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RestaurantReviews PRIMARY KEY DEFAULT NEWID(),
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Rating TINYINT NOT NULL,
        Comment NVARCHAR(MAX) NULL,
        VisitedAtUtc DATETIMEOFFSET NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantReviews_CreatedAtUtc DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_RestaurantReviews_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_RestaurantReviews_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT UQ_RestaurantReviews_RestaurantUserVisit UNIQUE (RestaurantId, UserId, VisitedAtUtc),
        CONSTRAINT CK_RestaurantReviews_RatingRange CHECK (Rating BETWEEN 1 AND 5),
        CONSTRAINT CK_RestaurantReviews_UpdatedAfterCreate CHECK (UpdatedAtUtc IS NULL OR UpdatedAtUtc >= CreatedAtUtc)
    );
END
GO

IF OBJECT_ID(N'dbo.MediaAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaAssets (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MediaAssets PRIMARY KEY DEFAULT NEWID(),
        OwnerUserId UNIQUEIDENTIFIER NOT NULL,
        ProfileUserId UNIQUEIDENTIFIER NULL,
        GroupId UNIQUEIDENTIFIER NULL,
        EventId UNIQUEIDENTIFIER NULL,
        StorageUrl NVARCHAR(2048) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MediaAssets_CreatedAtUtc DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_MediaAssets_OwnerUser FOREIGN KEY (OwnerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_MediaAssets_ProfileUser FOREIGN KEY (ProfileUserId) REFERENCES dbo.UserProfiles (UserId),
        CONSTRAINT FK_MediaAssets_Group FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_MediaAssets_Event FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT CK_MediaAssets_ExactlyOneContext CHECK (
            (CASE WHEN ProfileUserId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN GroupId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN EventId IS NULL THEN 0 ELSE 1 END) = 1
        ),
        CONSTRAINT CK_MediaAssets_NonBlankStorageUrl CHECK (LEN(LTRIM(RTRIM(StorageUrl))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.UserSearchHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSearchHistory (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserSearchHistory PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        SearchTerm NVARCHAR(255) NULL,
        Latitude FLOAT NULL,
        Longitude FLOAT NULL,
        SearchedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_UserSearchHistory_SearchedAtUtc DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_UserSearchHistory_UserAccounts FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_UserSearchHistory_Latitude CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
        CONSTRAINT CK_UserSearchHistory_Longitude CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
        CONSTRAINT CK_UserSearchHistory_CoordinatesTogether CHECK (
            (Latitude IS NULL AND Longitude IS NULL) OR
            (Latitude IS NOT NULL AND Longitude IS NOT NULL)
        )
    );
END
GO

IF OBJECT_ID(N'dbo.UserSearchHistoryCuisineFilters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSearchHistoryCuisineFilters (
        SearchHistoryId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UserSearchHistoryCuisineFilters PRIMARY KEY (SearchHistoryId, CuisineId),
        CONSTRAINT FK_UserSearchHistoryCuisineFilters_SearchHistory FOREIGN KEY (SearchHistoryId) REFERENCES dbo.UserSearchHistory (Id),
        CONSTRAINT FK_UserSearchHistoryCuisineFilters_Cuisines FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserFollows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFollows (
        FollowerUserId UNIQUEIDENTIFIER NOT NULL,
        FollowingUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_UserFollows_CreatedAtUtc DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT PK_UserFollows PRIMARY KEY (FollowerUserId, FollowingUserId),
        CONSTRAINT FK_UserFollows_FollowerUser FOREIGN KEY (FollowerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserFollows_FollowingUser FOREIGN KEY (FollowingUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_UserFollows_NoSelfFollow CHECK (FollowerUserId <> FollowingUserId)
    );
END
GO
