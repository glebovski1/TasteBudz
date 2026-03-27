PRAGMA foreign_keys = ON;

-----------------------------------------------------------------------
-- 1. AUTHENTICATION, IDENTITY & ACCESS
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS UserAccounts (
    Id TEXT NOT NULL PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    NormalizedUsername TEXT NOT NULL UNIQUE,
    Email TEXT NOT NULL UNIQUE,
    NormalizedEmail TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Status INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    DeletedAtUtc TEXT NULL
);

CREATE TABLE IF NOT EXISTS UserRoles (
    UserId TEXT NOT NULL,
    Role INTEGER NOT NULL,
    PRIMARY KEY (UserId, Role),
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

CREATE TABLE IF NOT EXISTS UserSessions (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    AccessToken TEXT NOT NULL,
    RefreshToken TEXT NOT NULL,
    ExpiresAtUtc TEXT NOT NULL,
    RefreshExpiresAtUtc TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    RevokedAtUtc TEXT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id),
    CHECK (RefreshExpiresAtUtc > ExpiresAtUtc),
    CHECK (RevokedAtUtc IS NULL OR RevokedAtUtc >= CreatedAtUtc)
);

-----------------------------------------------------------------------
-- 2. SHARED REFERENCE DATA
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Cuisines (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS ZipCoordinates (
    ZipCode TEXT NOT NULL PRIMARY KEY,
    Latitude REAL NOT NULL,
    Longitude REAL NOT NULL,
    CHECK (Latitude BETWEEN -90 AND 90),
    CHECK (Longitude BETWEEN -180 AND 180)
);

-----------------------------------------------------------------------
-- 3. PROFILES, PREFERENCES & PRIVACY
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS UserProfiles (
    UserId TEXT NOT NULL PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    Bio TEXT NULL,
    HomeAreaZipCode TEXT NOT NULL,
    SocialGoal INTEGER NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

CREATE TABLE IF NOT EXISTS UserPreferences (
    UserId TEXT NOT NULL PRIMARY KEY,
    SpiceTolerance INTEGER NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

CREATE TABLE IF NOT EXISTS UserCuisinePreferences (
    UserId TEXT NOT NULL,
    CuisineId TEXT NOT NULL,
    PRIMARY KEY (UserId, CuisineId),
    FOREIGN KEY (UserId) REFERENCES UserPreferences (UserId),
    FOREIGN KEY (CuisineId) REFERENCES Cuisines (Id)
);

CREATE TABLE IF NOT EXISTS UserDietaryFlags (
    UserId TEXT NOT NULL,
    DietaryFlag TEXT NOT NULL,
    PRIMARY KEY (UserId, DietaryFlag),
    FOREIGN KEY (UserId) REFERENCES UserPreferences (UserId),
    CHECK (trim(DietaryFlag) <> '')
);

CREATE TABLE IF NOT EXISTS UserAllergies (
    UserId TEXT NOT NULL,
    Allergy TEXT NOT NULL,
    PRIMARY KEY (UserId, Allergy),
    FOREIGN KEY (UserId) REFERENCES UserPreferences (UserId),
    CHECK (trim(Allergy) <> '')
);

CREATE TABLE IF NOT EXISTS PrivacySettings (
    UserId TEXT NOT NULL PRIMARY KEY,
    DiscoveryEnabled INTEGER NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

-----------------------------------------------------------------------
-- 4. AVAILABILITY (RECURRING & ONE-OFF)
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS RecurringAvailabilityWindows (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    DayOfWeek INTEGER NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT NOT NULL,
    Label TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id),
    CHECK (DayOfWeek BETWEEN 0 AND 6),
    CHECK (StartTime < EndTime)
);

CREATE TABLE IF NOT EXISTS OneOffAvailabilityWindows (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    StartsAtUtc TEXT NOT NULL,
    EndsAtUtc TEXT NOT NULL,
    Label TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id),
    CHECK (StartsAtUtc < EndsAtUtc)
);

-----------------------------------------------------------------------
-- 5. SOCIAL DISCOVERY, CONNECTIONS & SAFETY
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SwipeDecisions (
    ActorUserId TEXT NOT NULL,
    SubjectUserId TEXT NOT NULL,
    Decision INTEGER NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    PRIMARY KEY (ActorUserId, SubjectUserId),
    FOREIGN KEY (ActorUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (SubjectUserId) REFERENCES UserAccounts (Id),
    CHECK (ActorUserId <> SubjectUserId)
);

CREATE TABLE IF NOT EXISTS BudConnections (
    Id TEXT NOT NULL PRIMARY KEY,
    UserOneId TEXT NOT NULL,
    UserTwoId TEXT NOT NULL,
    State INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    EndedAtUtc TEXT NULL,
    FOREIGN KEY (UserOneId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (UserTwoId) REFERENCES UserAccounts (Id),
    UNIQUE (UserOneId, UserTwoId),
    CHECK (UserOneId <> UserTwoId),
    CHECK (UserOneId < UserTwoId),
    CHECK (EndedAtUtc IS NULL OR EndedAtUtc >= CreatedAtUtc)
);

CREATE TABLE IF NOT EXISTS UserBlocks (
    BlockerUserId TEXT NOT NULL,
    BlockedUserId TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    PRIMARY KEY (BlockerUserId, BlockedUserId),
    FOREIGN KEY (BlockerUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (BlockedUserId) REFERENCES UserAccounts (Id),
    CHECK (BlockerUserId <> BlockedUserId)
);

-----------------------------------------------------------------------
-- 6. GROUPS, MEMBERSHIP & INVITES
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Groups (
    Id TEXT NOT NULL PRIMARY KEY,
    OwnerUserId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT NULL,
    Visibility INTEGER NOT NULL,
    LifecycleState INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (OwnerUserId) REFERENCES UserAccounts (Id)
);

CREATE TABLE IF NOT EXISTS GroupMembers (
    GroupId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    State INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    PRIMARY KEY (GroupId, UserId),
    FOREIGN KEY (GroupId) REFERENCES Groups (Id),
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

CREATE TABLE IF NOT EXISTS GroupInvites (
    Id TEXT NOT NULL PRIMARY KEY,
    GroupId TEXT NOT NULL,
    InvitedUserId TEXT NOT NULL,
    InviterUserId TEXT NOT NULL,
    Status INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (GroupId) REFERENCES Groups (Id),
    FOREIGN KEY (InvitedUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (InviterUserId) REFERENCES UserAccounts (Id),
    CHECK (InvitedUserId <> InviterUserId)
);

-----------------------------------------------------------------------
-- 7. RESTAURANTS & EVENTS
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Restaurants (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    City TEXT NOT NULL,
    State TEXT NOT NULL,
    ZipCode TEXT NOT NULL,
    Latitude REAL NULL,
    Longitude REAL NULL,
    PriceTier INTEGER NOT NULL,
    ExternalPlaceId TEXT NULL,
    CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
    CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
    CHECK (
        (Latitude IS NULL AND Longitude IS NULL) OR
        (Latitude IS NOT NULL AND Longitude IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS RestaurantCuisines (
    RestaurantId TEXT NOT NULL,
    CuisineId TEXT NOT NULL,
    PRIMARY KEY (RestaurantId, CuisineId),
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants (Id),
    FOREIGN KEY (CuisineId) REFERENCES Cuisines (Id)
);

CREATE TABLE IF NOT EXISTS Events (
    Id TEXT NOT NULL PRIMARY KEY,
    HostUserId TEXT NOT NULL,
    Title TEXT NULL,
    EventType INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    EventStartAtUtc TEXT NOT NULL,
    DecisionAtUtc TEXT NOT NULL,
    Capacity INTEGER NOT NULL,
    MinParticipantsToRun INTEGER NOT NULL DEFAULT 2,
    SelectedRestaurantId TEXT NULL,
    CuisineTarget TEXT NULL,
    GroupId TEXT NULL,
    CancellationReason TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    CancelledAtUtc TEXT NULL,
    CompletedAtUtc TEXT NULL,
    FOREIGN KEY (HostUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (SelectedRestaurantId) REFERENCES Restaurants (Id),
    FOREIGN KEY (GroupId) REFERENCES Groups (Id),
    CHECK (Capacity BETWEEN 2 AND 8),
    CHECK (MinParticipantsToRun BETWEEN 2 AND Capacity),
    CHECK (DecisionAtUtc < EventStartAtUtc),
    CHECK (
        (CASE WHEN SelectedRestaurantId IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN NULLIF(trim(CuisineTarget), '') IS NULL THEN 0 ELSE 1 END) = 1
    ),
    CHECK (CancelledAtUtc IS NULL OR CancelledAtUtc >= CreatedAtUtc),
    CHECK (CompletedAtUtc IS NULL OR CompletedAtUtc >= EventStartAtUtc)
);

CREATE TABLE IF NOT EXISTS EventParticipants (
    EventId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    State INTEGER NOT NULL,
    InvitedAtUtc TEXT NULL,
    JoinedAtUtc TEXT NULL,
    RespondedAtUtc TEXT NULL,
    LeftAtUtc TEXT NULL,
    RemovedAtUtc TEXT NULL,
    PRIMARY KEY (EventId, UserId),
    FOREIGN KEY (EventId) REFERENCES Events (Id),
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id)
);

-----------------------------------------------------------------------
-- 8. CHAT & NOTIFICATIONS
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ChatThreads (
    Id TEXT NOT NULL PRIMARY KEY,
    ScopeType INTEGER NOT NULL,
    ScopeId TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UNIQUE (ScopeType, ScopeId)
);

CREATE TABLE IF NOT EXISTS ChatMessages (
    Id TEXT NOT NULL PRIMARY KEY,
    ThreadId TEXT NOT NULL,
    SenderUserId TEXT NOT NULL,
    Body TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (ThreadId) REFERENCES ChatThreads (Id),
    FOREIGN KEY (SenderUserId) REFERENCES UserAccounts (Id),
    CHECK (trim(Body) <> '')
);

CREATE TABLE IF NOT EXISTS Notifications (
    Id TEXT NOT NULL PRIMARY KEY,
    RecipientUserId TEXT NOT NULL,
    NotificationType INTEGER NOT NULL,
    ContextType TEXT NOT NULL,
    ContextId TEXT NULL,
    Message TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    ReadAtUtc TEXT NULL,
    FOREIGN KEY (RecipientUserId) REFERENCES UserAccounts (Id),
    CHECK (trim(ContextType) <> ''),
    CHECK (trim(Message) <> ''),
    CHECK (ReadAtUtc IS NULL OR ReadAtUtc >= CreatedAtUtc)
);

-----------------------------------------------------------------------
-- 9. MODERATION, RESTRICTIONS & AUDIT
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ModerationReports (
    Id TEXT NOT NULL PRIMARY KEY,
    ReporterUserId TEXT NOT NULL,
    TargetType INTEGER NOT NULL,
    TargetId TEXT NOT NULL,
    Category TEXT NOT NULL,
    Reason TEXT NOT NULL,
    Explanation TEXT NULL,
    RelatedEventId TEXT NULL,
    RelatedUserId TEXT NULL,
    RelatedMessageId TEXT NULL,
    Status INTEGER NOT NULL,
    ResolvedByUserId TEXT NULL,
    ResolvedAtUtc TEXT NULL,
    ResolutionDecision TEXT NULL,
    ResolutionNotes TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (ReporterUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (RelatedEventId) REFERENCES Events (Id),
    FOREIGN KEY (RelatedUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (RelatedMessageId) REFERENCES ChatMessages (Id),
    FOREIGN KEY (ResolvedByUserId) REFERENCES UserAccounts (Id),
    CHECK (trim(Category) <> ''),
    CHECK (trim(Reason) <> ''),
    CHECK (ResolvedAtUtc IS NULL OR ResolvedAtUtc >= CreatedAtUtc)
);

CREATE TABLE IF NOT EXISTS ModerationActions (
    Id TEXT NOT NULL PRIMARY KEY,
    ActorUserId TEXT NOT NULL,
    ReportId TEXT NULL,
    ActionType INTEGER NOT NULL,
    Notes TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    FOREIGN KEY (ActorUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (ReportId) REFERENCES ModerationReports (Id),
    CHECK (trim(Notes) <> '')
);

CREATE TABLE IF NOT EXISTS UserRestrictions (
    Id TEXT NOT NULL PRIMARY KEY,
    SubjectUserId TEXT NOT NULL,
    IssuedByUserId TEXT NOT NULL,
    ModerationActionId TEXT NULL,
    Scope INTEGER NOT NULL,
    Reason TEXT NOT NULL,
    StartsAtUtc TEXT NOT NULL,
    ExpiresAtUtc TEXT NULL,
    Status INTEGER NOT NULL,
    RevokedAtUtc TEXT NULL,
    FOREIGN KEY (SubjectUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (IssuedByUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (ModerationActionId) REFERENCES ModerationActions (Id),
    CHECK (trim(Reason) <> ''),
    CHECK (ExpiresAtUtc IS NULL OR ExpiresAtUtc > StartsAtUtc),
    CHECK (RevokedAtUtc IS NULL OR RevokedAtUtc >= StartsAtUtc)
);

CREATE TABLE IF NOT EXISTS AuditLogEntries (
    Id TEXT NOT NULL PRIMARY KEY,
    ActionType TEXT NOT NULL,
    ActorUserId TEXT NOT NULL,
    TargetEntityType TEXT NOT NULL,
    TargetEntityId TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    Details TEXT NOT NULL,
    FOREIGN KEY (ActorUserId) REFERENCES UserAccounts (Id),
    CHECK (trim(ActionType) <> ''),
    CHECK (trim(TargetEntityType) <> ''),
    CHECK (trim(Details) <> '')
);

-----------------------------------------------------------------------
-- 10. FUTURE EXTENSION TABLES (NOT USED BY CURRENT MVP FLOWS)
-----------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS RestaurantReviews (
    Id TEXT NOT NULL PRIMARY KEY,
    RestaurantId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Rating INTEGER NOT NULL,
    Comment TEXT NULL,
    VisitedAtUtc TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NULL,
    FOREIGN KEY (RestaurantId) REFERENCES Restaurants (Id),
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id),
    UNIQUE (RestaurantId, UserId, VisitedAtUtc),
    CHECK (Rating BETWEEN 1 AND 5),
    CHECK (UpdatedAtUtc IS NULL OR UpdatedAtUtc >= CreatedAtUtc)
);

CREATE TABLE IF NOT EXISTS MediaAssets (
    Id TEXT NOT NULL PRIMARY KEY,
    OwnerUserId TEXT NOT NULL,
    ProfileUserId TEXT NULL,
    GroupId TEXT NULL,
    EventId TEXT NULL,
    StorageUrl TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OwnerUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (ProfileUserId) REFERENCES UserProfiles (UserId),
    FOREIGN KEY (GroupId) REFERENCES Groups (Id),
    FOREIGN KEY (EventId) REFERENCES Events (Id),
    CHECK (
        (CASE WHEN ProfileUserId IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN GroupId IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN EventId IS NULL THEN 0 ELSE 1 END) = 1
    ),
    CHECK (trim(StorageUrl) <> '')
);

CREATE TABLE IF NOT EXISTS UserSearchHistory (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    SearchTerm TEXT NULL,
    Latitude REAL NULL,
    Longitude REAL NULL,
    SearchedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES UserAccounts (Id),
    CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
    CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
    CHECK (
        (Latitude IS NULL AND Longitude IS NULL) OR
        (Latitude IS NOT NULL AND Longitude IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS UserSearchHistoryCuisineFilters (
    SearchHistoryId TEXT NOT NULL,
    CuisineId TEXT NOT NULL,
    PRIMARY KEY (SearchHistoryId, CuisineId),
    FOREIGN KEY (SearchHistoryId) REFERENCES UserSearchHistory (Id),
    FOREIGN KEY (CuisineId) REFERENCES Cuisines (Id)
);

CREATE TABLE IF NOT EXISTS UserFollows (
    FollowerUserId TEXT NOT NULL,
    FollowingUserId TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (FollowerUserId, FollowingUserId),
    FOREIGN KEY (FollowerUserId) REFERENCES UserAccounts (Id),
    FOREIGN KEY (FollowingUserId) REFERENCES UserAccounts (Id),
    CHECK (FollowerUserId <> FollowingUserId)
);

-----------------------------------------------------------------------
-- 11. INDEXES FOR CURRENT MVP QUERY PATHS
-----------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS IX_UserSessions_AccessToken ON UserSessions (AccessToken);
CREATE UNIQUE INDEX IF NOT EXISTS IX_UserSessions_RefreshToken ON UserSessions (RefreshToken);
CREATE INDEX IF NOT EXISTS IX_GroupMembers_UserId ON GroupMembers (UserId);
CREATE INDEX IF NOT EXISTS IX_GroupInvites_InvitedUserId ON GroupInvites (InvitedUserId);
CREATE INDEX IF NOT EXISTS IX_Restaurants_ZipCode ON Restaurants (ZipCode);
CREATE INDEX IF NOT EXISTS IX_Events_GroupId ON Events (GroupId);
CREATE INDEX IF NOT EXISTS IX_EventParticipants_UserId ON EventParticipants (UserId);
CREATE INDEX IF NOT EXISTS IX_ChatMessages_ThreadId_CreatedAtUtc_Id ON ChatMessages (ThreadId, CreatedAtUtc, Id);
CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientUserId_CreatedAtUtc ON Notifications (RecipientUserId, CreatedAtUtc);
CREATE INDEX IF NOT EXISTS IX_ModerationReports_Status_CreatedAtUtc ON ModerationReports (Status, CreatedAtUtc);
CREATE INDEX IF NOT EXISTS IX_UserRestrictions_SubjectUserId_Scope_Status ON UserRestrictions (SubjectUserId, Scope, Status);
