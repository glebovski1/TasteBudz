SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAccounts (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY,
        Username NVARCHAR(80) NOT NULL,
        NormalizedUsername NVARCHAR(80) NOT NULL,
        Email NVARCHAR(320) NOT NULL,
        NormalizedEmail NVARCHAR(320) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        DeletedAtUtc DATETIMEOFFSET NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserAccounts_Username' AND object_id = OBJECT_ID(N'dbo.UserAccounts'))
    CREATE UNIQUE INDEX UX_UserAccounts_Username ON dbo.UserAccounts (Username);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserAccounts_NormalizedUsername' AND object_id = OBJECT_ID(N'dbo.UserAccounts'))
    CREATE UNIQUE INDEX UX_UserAccounts_NormalizedUsername ON dbo.UserAccounts (NormalizedUsername);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserAccounts_Email' AND object_id = OBJECT_ID(N'dbo.UserAccounts'))
    CREATE UNIQUE INDEX UX_UserAccounts_Email ON dbo.UserAccounts (Email);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserAccounts_NormalizedEmail' AND object_id = OBJECT_ID(N'dbo.UserAccounts'))
    CREATE UNIQUE INDEX UX_UserAccounts_NormalizedEmail ON dbo.UserAccounts (NormalizedEmail);
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserId UNIQUEIDENTIFIER NOT NULL,
        Role INT NOT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, Role),
        CONSTRAINT FK_UserRoles_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserSessions PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        AccessToken NVARCHAR(512) NOT NULL,
        RefreshToken NVARCHAR(512) NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET NOT NULL,
        RefreshExpiresAtUtc DATETIMEOFFSET NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_UserSessions_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSessions_AccessToken' AND object_id = OBJECT_ID(N'dbo.UserSessions'))
    CREATE UNIQUE INDEX IX_UserSessions_AccessToken ON dbo.UserSessions (AccessToken);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserSessions_RefreshToken' AND object_id = OBJECT_ID(N'dbo.UserSessions'))
    CREATE UNIQUE INDEX IX_UserSessions_RefreshToken ON dbo.UserSessions (RefreshToken);
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash NVARCHAR(128) NOT NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET NOT NULL,
        UsedAtUtc DATETIMEOFFSET NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_PasswordResetTokens_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_PasswordResetTokens_UserAccounts_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PasswordResetTokens_TokenHash' AND object_id = OBJECT_ID(N'dbo.PasswordResetTokens'))
    CREATE UNIQUE INDEX UX_PasswordResetTokens_TokenHash ON dbo.PasswordResetTokens (TokenHash);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetTokens_UserId_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.PasswordResetTokens'))
    CREATE INDEX IX_PasswordResetTokens_UserId_CreatedAtUtc ON dbo.PasswordResetTokens (UserId, CreatedAtUtc);
GO

IF OBJECT_ID(N'dbo.Cuisines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cuisines (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Cuisines PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL CONSTRAINT UX_Cuisines_Name UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.ZipCoordinates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZipCoordinates (
        ZipCode NVARCHAR(10) NOT NULL CONSTRAINT PK_ZipCoordinates PRIMARY KEY,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.UserProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserProfiles (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserProfiles PRIMARY KEY,
        DisplayName NVARCHAR(120) NOT NULL,
        Bio NVARCHAR(MAX) NULL,
        HomeAreaZipCode NVARCHAR(10) NOT NULL,
        SocialGoal INT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_UserProfiles_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPreferences (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY,
        SpiceTolerance INT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_UserPreferences_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserCuisinePreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCuisinePreferences (
        UserId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UserCuisinePreferences PRIMARY KEY (UserId, CuisineId),
        CONSTRAINT FK_UserCuisinePreferences_UserPreferences_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId),
        CONSTRAINT FK_UserCuisinePreferences_Cuisines_CuisineId FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserDietaryFlags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDietaryFlags (
        UserId UNIQUEIDENTIFIER NOT NULL,
        DietaryFlag NVARCHAR(120) NOT NULL,
        CONSTRAINT PK_UserDietaryFlags PRIMARY KEY (UserId, DietaryFlag),
        CONSTRAINT FK_UserDietaryFlags_UserPreferences_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserAllergies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAllergies (
        UserId UNIQUEIDENTIFIER NOT NULL,
        Allergy NVARCHAR(120) NOT NULL,
        CONSTRAINT PK_UserAllergies PRIMARY KEY (UserId, Allergy),
        CONSTRAINT FK_UserAllergies_UserPreferences_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserPreferences (UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.PrivacySettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivacySettings (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PrivacySettings PRIMARY KEY,
        DiscoveryEnabled BIT NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_PrivacySettings_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.RecurringAvailabilityWindows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecurringAvailabilityWindows (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RecurringAvailabilityWindows PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        DayOfWeek INT NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        Label NVARCHAR(120) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_RecurringAvailabilityWindows_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.OneOffAvailabilityWindows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OneOffAvailabilityWindows (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OneOffAvailabilityWindows PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        StartsAtUtc DATETIMEOFFSET NOT NULL,
        EndsAtUtc DATETIMEOFFSET NOT NULL,
        Label NVARCHAR(120) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_OneOffAvailabilityWindows_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.SwipeDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SwipeDecisions (
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        SubjectUserId UNIQUEIDENTIFIER NOT NULL,
        Decision INT NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT PK_SwipeDecisions PRIMARY KEY (ActorUserId, SubjectUserId),
        CONSTRAINT FK_SwipeDecisions_UserAccounts_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_SwipeDecisions_UserAccounts_Subject FOREIGN KEY (SubjectUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
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
        CONSTRAINT FK_BudConnections_UserAccounts_UserOne FOREIGN KEY (UserOneId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_BudConnections_UserAccounts_UserTwo FOREIGN KEY (UserTwoId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BudConnections_UserPair' AND object_id = OBJECT_ID(N'dbo.BudConnections'))
    CREATE UNIQUE INDEX UX_BudConnections_UserPair ON dbo.BudConnections (UserOneId, UserTwoId);
GO

IF OBJECT_ID(N'dbo.UserBlocks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserBlocks (
        BlockerUserId UNIQUEIDENTIFIER NOT NULL,
        BlockedUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT PK_UserBlocks PRIMARY KEY (BlockerUserId, BlockedUserId),
        CONSTRAINT FK_UserBlocks_UserAccounts_Blocker FOREIGN KEY (BlockerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserBlocks_UserAccounts_Blocked FOREIGN KEY (BlockedUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Groups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Groups (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Groups PRIMARY KEY,
        OwnerUserId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(160) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Visibility INT NOT NULL,
        LifecycleState INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_Groups_UserAccounts_OwnerUserId FOREIGN KEY (OwnerUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
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
        CONSTRAINT FK_GroupMembers_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_GroupMembers_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GroupMembers_UserId' AND object_id = OBJECT_ID(N'dbo.GroupMembers'))
    CREATE INDEX IX_GroupMembers_UserId ON dbo.GroupMembers (UserId);
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
        CONSTRAINT FK_GroupInvites_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_GroupInvites_UserAccounts_InvitedUserId FOREIGN KEY (InvitedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_GroupInvites_UserAccounts_InviterUserId FOREIGN KEY (InviterUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GroupInvites_InvitedUserId' AND object_id = OBJECT_ID(N'dbo.GroupInvites'))
    CREATE INDEX IX_GroupInvites_InvitedUserId ON dbo.GroupInvites (InvitedUserId);
GO

IF OBJECT_ID(N'dbo.Restaurants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Restaurants (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Restaurants PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        City NVARCHAR(80) NOT NULL,
        State NVARCHAR(2) NOT NULL,
        ZipCode NVARCHAR(10) NOT NULL,
        Latitude FLOAT NULL,
        Longitude FLOAT NULL,
        PriceTier INT NOT NULL,
        ExternalPlaceId NVARCHAR(160) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Restaurants_ZipCode' AND object_id = OBJECT_ID(N'dbo.Restaurants'))
    CREATE INDEX IX_Restaurants_ZipCode ON dbo.Restaurants (ZipCode);
GO

IF OBJECT_ID(N'dbo.RestaurantCuisines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantCuisines (
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_RestaurantCuisines PRIMARY KEY (RestaurantId, CuisineId),
        CONSTRAINT FK_RestaurantCuisines_Restaurants_RestaurantId FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_RestaurantCuisines_Cuisines_CuisineId FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.RestaurantAdminAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantAdminAssignments (
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT PK_RestaurantAdminAssignments PRIMARY KEY (RestaurantId, UserId),
        CONSTRAINT FK_RestaurantAdminAssignments_Restaurants_RestaurantId FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_RestaurantAdminAssignments_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RestaurantAdminAssignments_UserId' AND object_id = OBJECT_ID(N'dbo.RestaurantAdminAssignments'))
    CREATE INDEX IX_RestaurantAdminAssignments_UserId ON dbo.RestaurantAdminAssignments (UserId);
GO

IF OBJECT_ID(N'dbo.RestaurantSlots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantSlots (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RestaurantSlots PRIMARY KEY,
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        StartsAtUtc DATETIMEOFFSET NOT NULL,
        EndsAtUtc DATETIMEOFFSET NOT NULL,
        Capacity INT NOT NULL,
        CutoffAtUtc DATETIMEOFFSET NOT NULL,
        MinThresholdForDiscount INT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CancelledAtUtc DATETIMEOFFSET NULL,
        CancellationReason NVARCHAR(250) NULL,
        CONSTRAINT FK_RestaurantSlots_Restaurants_RestaurantId FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RestaurantSlots_RestaurantId_StartsAtUtc' AND object_id = OBJECT_ID(N'dbo.RestaurantSlots'))
    CREATE INDEX IX_RestaurantSlots_RestaurantId_StartsAtUtc ON dbo.RestaurantSlots (RestaurantId, StartsAtUtc);
GO

IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
        HostUserId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(160) NULL,
        EventType INT NOT NULL,
        Status INT NOT NULL,
        EventStartAtUtc DATETIMEOFFSET NOT NULL,
        DecisionAtUtc DATETIMEOFFSET NOT NULL,
        Capacity INT NOT NULL,
        MinParticipantsToRun INT NOT NULL CONSTRAINT DF_Events_MinParticipantsToRun DEFAULT 2,
        SelectedRestaurantId UNIQUEIDENTIFIER NULL,
        CuisineTarget NVARCHAR(120) NULL,
        GroupId UNIQUEIDENTIFIER NULL,
        CancellationReason NVARCHAR(250) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CancelledAtUtc DATETIMEOFFSET NULL,
        CompletedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_Events_UserAccounts_HostUserId FOREIGN KEY (HostUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_Events_Restaurants_SelectedRestaurantId FOREIGN KEY (SelectedRestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_Events_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Events_GroupId' AND object_id = OBJECT_ID(N'dbo.Events'))
    CREATE INDEX IX_Events_GroupId ON dbo.Events (GroupId);
GO

IF OBJECT_ID(N'dbo.EventSlotReservations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventSlotReservations (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EventSlotReservations PRIMARY KEY,
        EventId UNIQUEIDENTIFIER NOT NULL,
        SlotId UNIQUEIDENTIFIER NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CancelledAtUtc DATETIMEOFFSET NULL,
        CancellationReason NVARCHAR(250) NULL,
        CONSTRAINT FK_EventSlotReservations_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_EventSlotReservations_RestaurantSlots_SlotId FOREIGN KEY (SlotId) REFERENCES dbo.RestaurantSlots (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EventSlotReservations_Event_Active' AND object_id = OBJECT_ID(N'dbo.EventSlotReservations'))
    CREATE UNIQUE INDEX UX_EventSlotReservations_Event_Active ON dbo.EventSlotReservations (EventId) WHERE Status = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EventSlotReservations_Slot_Active' AND object_id = OBJECT_ID(N'dbo.EventSlotReservations'))
    CREATE UNIQUE INDEX UX_EventSlotReservations_Slot_Active ON dbo.EventSlotReservations (SlotId) WHERE Status = 0;
GO

IF OBJECT_ID(N'dbo.DiscountActivations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiscountActivations (
        ReservationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DiscountActivations PRIMARY KEY,
        IsActive BIT NOT NULL,
        IsFinalized BIT NOT NULL,
        EvaluatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_DiscountActivations_EventSlotReservations_ReservationId FOREIGN KEY (ReservationId) REFERENCES dbo.EventSlotReservations (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.CheckoutSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CheckoutSessions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CheckoutSessions PRIMARY KEY,
        EventId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Status INT NOT NULL,
        Currency NVARCHAR(3) NOT NULL,
        SubtotalCents INT NOT NULL,
        DiscountCents INT NOT NULL,
        TotalCents INT NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        CompletedAtUtc DATETIMEOFFSET NULL,
        CancelledAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_CheckoutSessions_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_CheckoutSessions_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CheckoutSessions_EventId_UserId_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.CheckoutSessions'))
    CREATE INDEX IX_CheckoutSessions_EventId_UserId_CreatedAtUtc ON dbo.CheckoutSessions (EventId, UserId, CreatedAtUtc);
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
        CONSTRAINT FK_EventParticipants_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_EventParticipants_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EventParticipants_UserId' AND object_id = OBJECT_ID(N'dbo.EventParticipants'))
    CREATE INDEX IX_EventParticipants_UserId ON dbo.EventParticipants (UserId);
GO

IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatThreads (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChatThreads PRIMARY KEY,
        ScopeType INT NOT NULL,
        ScopeId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ChatThreads_Scope' AND object_id = OBJECT_ID(N'dbo.ChatThreads'))
    CREATE UNIQUE INDEX UX_ChatThreads_Scope ON dbo.ChatThreads (ScopeType, ScopeId);
GO

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ThreadId UNIQUEIDENTIFIER NOT NULL,
        SenderUserId UNIQUEIDENTIFIER NOT NULL,
        Body NVARCHAR(500) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_ChatMessages_ChatThreads_ThreadId FOREIGN KEY (ThreadId) REFERENCES dbo.ChatThreads (Id),
        CONSTRAINT FK_ChatMessages_UserAccounts_SenderUserId FOREIGN KEY (SenderUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatMessages_ThreadId_CreatedAtUtc_Id' AND object_id = OBJECT_ID(N'dbo.ChatMessages'))
    CREATE INDEX IX_ChatMessages_ThreadId_CreatedAtUtc_Id ON dbo.ChatMessages (ThreadId, CreatedAtUtc, Id);
GO

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
        RecipientUserId UNIQUEIDENTIFIER NOT NULL,
        NotificationType INT NOT NULL,
        ContextType NVARCHAR(80) NOT NULL,
        ContextId UNIQUEIDENTIFIER NULL,
        Message NVARCHAR(500) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        ReadAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_Notifications_UserAccounts_RecipientUserId FOREIGN KEY (RecipientUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notifications_RecipientUserId_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.Notifications'))
    CREATE INDEX IX_Notifications_RecipientUserId_CreatedAtUtc ON dbo.Notifications (RecipientUserId, CreatedAtUtc);
GO

IF OBJECT_ID(N'dbo.ModerationReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationReports (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ModerationReports PRIMARY KEY,
        ReporterUserId UNIQUEIDENTIFIER NOT NULL,
        TargetType INT NOT NULL,
        TargetId UNIQUEIDENTIFIER NOT NULL,
        Category NVARCHAR(120) NOT NULL,
        Reason NVARCHAR(250) NOT NULL,
        Explanation NVARCHAR(MAX) NULL,
        RelatedEventId UNIQUEIDENTIFIER NULL,
        RelatedUserId UNIQUEIDENTIFIER NULL,
        RelatedMessageId UNIQUEIDENTIFIER NULL,
        Status INT NOT NULL,
        ResolvedByUserId UNIQUEIDENTIFIER NULL,
        ResolvedAtUtc DATETIMEOFFSET NULL,
        ResolutionDecision NVARCHAR(120) NULL,
        ResolutionNotes NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_ModerationReports_UserAccounts_ReporterUserId FOREIGN KEY (ReporterUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationReports_Events_RelatedEventId FOREIGN KEY (RelatedEventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_ModerationReports_UserAccounts_RelatedUserId FOREIGN KEY (RelatedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationReports_ChatMessages_RelatedMessageId FOREIGN KEY (RelatedMessageId) REFERENCES dbo.ChatMessages (Id),
        CONSTRAINT FK_ModerationReports_UserAccounts_ResolvedByUserId FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ModerationReports_Status_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.ModerationReports'))
    CREATE INDEX IX_ModerationReports_Status_CreatedAtUtc ON dbo.ModerationReports (Status, CreatedAtUtc);
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
        CONSTRAINT FK_ModerationActions_UserAccounts_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_ModerationActions_ModerationReports_ReportId FOREIGN KEY (ReportId) REFERENCES dbo.ModerationReports (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserRestrictions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRestrictions (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserRestrictions PRIMARY KEY,
        SubjectUserId UNIQUEIDENTIFIER NOT NULL,
        IssuedByUserId UNIQUEIDENTIFIER NOT NULL,
        ModerationActionId UNIQUEIDENTIFIER NULL,
        Scope INT NOT NULL,
        Reason NVARCHAR(250) NOT NULL,
        StartsAtUtc DATETIMEOFFSET NOT NULL,
        ExpiresAtUtc DATETIMEOFFSET NULL,
        Status INT NOT NULL,
        RevokedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_UserRestrictions_UserAccounts_SubjectUserId FOREIGN KEY (SubjectUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserRestrictions_UserAccounts_IssuedByUserId FOREIGN KEY (IssuedByUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserRestrictions_ModerationActions_ModerationActionId FOREIGN KEY (ModerationActionId) REFERENCES dbo.ModerationActions (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserRestrictions_SubjectUserId_Scope_Status' AND object_id = OBJECT_ID(N'dbo.UserRestrictions'))
    CREATE INDEX IX_UserRestrictions_SubjectUserId_Scope_Status ON dbo.UserRestrictions (SubjectUserId, Scope, Status);
GO

IF OBJECT_ID(N'dbo.AuditLogEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogEntries (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditLogEntries PRIMARY KEY,
        ActionType NVARCHAR(120) NOT NULL,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        TargetEntityType NVARCHAR(120) NOT NULL,
        TargetEntityId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        Details NVARCHAR(MAX) NOT NULL,
        CONSTRAINT FK_AuditLogEntries_UserAccounts_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.RestaurantReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RestaurantReviews (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RestaurantReviews PRIMARY KEY,
        RestaurantId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Rating INT NOT NULL,
        Comment NVARCHAR(MAX) NULL,
        VisitedAtUtc DATETIMEOFFSET NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantReviews_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_RestaurantReviews_Restaurants_RestaurantId FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT FK_RestaurantReviews_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RestaurantReviews_Restaurant_User_Visited' AND object_id = OBJECT_ID(N'dbo.RestaurantReviews'))
    CREATE UNIQUE INDEX UX_RestaurantReviews_Restaurant_User_Visited ON dbo.RestaurantReviews (RestaurantId, UserId, VisitedAtUtc);
GO

IF OBJECT_ID(N'dbo.MediaAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaAssets (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MediaAssets PRIMARY KEY,
        OwnerUserId UNIQUEIDENTIFIER NOT NULL,
        ProfileUserId UNIQUEIDENTIFIER NULL,
        GroupId UNIQUEIDENTIFIER NULL,
        EventId UNIQUEIDENTIFIER NULL,
        ReportId UNIQUEIDENTIFIER NULL,
        OriginalFileName NVARCHAR(260) NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        ContentLength BIGINT NOT NULL,
        Content VARBINARY(MAX) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MediaAssets_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MediaAssets_UserAccounts_OwnerUserId FOREIGN KEY (OwnerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_MediaAssets_UserProfiles_ProfileUserId FOREIGN KEY (ProfileUserId) REFERENCES dbo.UserProfiles (UserId),
        CONSTRAINT FK_MediaAssets_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_MediaAssets_Events_EventId FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
        CONSTRAINT FK_MediaAssets_ModerationReports_ReportId FOREIGN KEY (ReportId) REFERENCES dbo.ModerationReports (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaAssets_ProfileUserId' AND object_id = OBJECT_ID(N'dbo.MediaAssets'))
    CREATE INDEX IX_MediaAssets_ProfileUserId ON dbo.MediaAssets (ProfileUserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaAssets_ReportId' AND object_id = OBJECT_ID(N'dbo.MediaAssets'))
    CREATE INDEX IX_MediaAssets_ReportId ON dbo.MediaAssets (ReportId);
GO

IF OBJECT_ID(N'dbo.UserSearchHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSearchHistory (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserSearchHistory PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        SearchTerm NVARCHAR(200) NULL,
        Latitude FLOAT NULL,
        Longitude FLOAT NULL,
        SearchedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_UserSearchHistory_SearchedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_UserSearchHistory_UserAccounts_UserId FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserSearchHistoryCuisineFilters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSearchHistoryCuisineFilters (
        SearchHistoryId UNIQUEIDENTIFIER NOT NULL,
        CuisineId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UserSearchHistoryCuisineFilters PRIMARY KEY (SearchHistoryId, CuisineId),
        CONSTRAINT FK_UserSearchHistoryCuisineFilters_UserSearchHistory_SearchHistoryId FOREIGN KEY (SearchHistoryId) REFERENCES dbo.UserSearchHistory (Id),
        CONSTRAINT FK_UserSearchHistoryCuisineFilters_Cuisines_CuisineId FOREIGN KEY (CuisineId) REFERENCES dbo.Cuisines (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserFollows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFollows (
        FollowerUserId UNIQUEIDENTIFIER NOT NULL,
        FollowingUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_UserFollows_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_UserFollows PRIMARY KEY (FollowerUserId, FollowingUserId),
        CONSTRAINT FK_UserFollows_UserAccounts_FollowerUserId FOREIGN KEY (FollowerUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_UserFollows_UserAccounts_FollowingUserId FOREIGN KEY (FollowingUserId) REFERENCES dbo.UserAccounts (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260412-010-schema')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260412-010-schema', N'Initial Azure SQL schema aligned to the current TasteBudz SQLite schema.');
END;
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260416-password-reset-tokens')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260416-password-reset-tokens', N'Add admin-issued password reset token storage.');
END;
GO
