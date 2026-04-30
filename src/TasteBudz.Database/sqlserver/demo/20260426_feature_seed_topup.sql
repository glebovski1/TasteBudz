-- Optional Azure SQL feature-coverage top-up for capstone demos.
-- Run only after the normal SQL Server schema, reference seed, and current patches are applied.
-- This script is guarded by fixed GUIDs and is intentionally separate from production bootstrap.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @PasswordHash NVARCHAR(MAX) = N'v1.100000.AQIDBAUGBwgJCgsMDQ4PEA==.LKdRd9dYH++R4m+ceQJ54Afaf3rrbhrxhZD9AkK3WVQ=';
DECLARE @Alex UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000101');
DECLARE @Brooke UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000102');
DECLARE @Mod UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000103');
DECLARE @Admin UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000104');
DECLARE @RestaurantAdmin UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000105');
DECLARE @BudConnection UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000001201');
DECLARE @PublicGroup UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002001');
DECLARE @PrivateGroup UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002002');
DECLARE @OpenEvent UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000003001');
DECLARE @CompletedEvent UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000003002');
DECLARE @Feedback UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000003901');
DECLARE @EventThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004001');
DECLARE @GroupThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004002');
DECLARE @DirectThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004003');
DECLARE @SupportThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004004');
DECLARE @Report UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000007001');
DECLARE @Action UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000008001');
DECLARE @Restriction UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000008101');
DECLARE @Slot UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009001');
DECLARE @Reservation UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009101');
DECLARE @AvatarMedia UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000010001');
DECLARE @ReportMedia UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000010002');
DECLARE @FeedbackMedia UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000010003');
-- Complete demo feedback PNG; earlier top-up rows may contain only the PNG signature.
DECLARE @DemoFeedbackPng VARBINARY(MAX) = 0x89504E470D0A1A0A0000000D494844520000003000000020080600000054D4FB1C000000B04944415478DAEDD8B10980401004C00FADC3DC026CC3D47AACC8AE046343C5C0C740E4EFFC3D56D86013F1E1063D583E0D7DB7BF65999B9C761A73EECF9F12F56E2A05300E5F0C601DBE08C03CBC09C0387C31807578D312330EFF19C0B01F6EC0BEADD5E381BA00E74104C0F395CC80EB201260F9C592B74AA000615502B5036155221A50BD4A44022055220A00AB12110068954003E05502095095509550955095D0AD04CFAD047B041040809F030E30943100E1EB15040000000049454E44AE426082;

IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @Alex)
    INSERT INTO dbo.UserAccounts (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (@Alex, N'tb_demo_alex', N'TB_DEMO_ALEX', N'tb_demo_alex@tastebudz.local', N'TB_DEMO_ALEX@TASTEBUDZ.LOCAL', @PasswordHash, 0, '2026-04-26T12:00:00Z', '2026-04-26T12:00:00Z', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @Brooke)
    INSERT INTO dbo.UserAccounts (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (@Brooke, N'tb_demo_brooke', N'TB_DEMO_BROOKE', N'tb_demo_brooke@tastebudz.local', N'TB_DEMO_BROOKE@TASTEBUDZ.LOCAL', @PasswordHash, 0, '2026-04-26T12:01:00Z', '2026-04-26T12:01:00Z', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @Mod)
    INSERT INTO dbo.UserAccounts (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (@Mod, N'tb_demo_mod', N'TB_DEMO_MOD', N'tb_demo_mod@tastebudz.local', N'TB_DEMO_MOD@TASTEBUDZ.LOCAL', @PasswordHash, 0, '2026-04-26T12:02:00Z', '2026-04-26T12:02:00Z', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @Admin)
    INSERT INTO dbo.UserAccounts (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (@Admin, N'tb_demo_admin', N'TB_DEMO_ADMIN', N'tb_demo_admin@tastebudz.local', N'TB_DEMO_ADMIN@TASTEBUDZ.LOCAL', @PasswordHash, 0, '2026-04-26T12:03:00Z', '2026-04-26T12:03:00Z', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @RestaurantAdmin)
    INSERT INTO dbo.UserAccounts (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (@RestaurantAdmin, N'tb_demo_restaurant', N'TB_DEMO_RESTAURANT', N'tb_demo_restaurant@tastebudz.local', N'TB_DEMO_RESTAURANT@TASTEBUDZ.LOCAL', @PasswordHash, 0, '2026-04-26T12:04:00Z', '2026-04-26T12:04:00Z', NULL);

MERGE dbo.UserRoles AS target
USING (VALUES (@Alex, 0), (@Brooke, 0), (@Mod, 0), (@Mod, 1), (@Admin, 0), (@Admin, 2), (@RestaurantAdmin, 0), (@RestaurantAdmin, 3)) AS source (UserId, Role)
ON target.UserId = source.UserId AND target.Role = source.Role
WHEN NOT MATCHED THEN INSERT (UserId, Role) VALUES (source.UserId, source.Role);

MERGE dbo.UserProfiles AS target
USING (VALUES
    (@Alex, N'Demo Alex', N'Local and Azure demo account for core social flows.', N'45220', 0, '2026-04-26T12:10:00Z', '2026-04-26T12:10:00Z'),
    (@Brooke, N'Demo Brooke', N'Food friend for Budz, events, chat, and feedback.', N'45202', 1, '2026-04-26T12:11:00Z', '2026-04-26T12:11:00Z'),
    (@Mod, N'Demo Moderator', N'Moderator account for report review.', N'45219', 2, '2026-04-26T12:12:00Z', '2026-04-26T12:12:00Z'),
    (@Admin, N'Demo Admin', N'Admin account for reset and moderation workflows.', N'41011', 2, '2026-04-26T12:13:00Z', '2026-04-26T12:13:00Z'),
    (@RestaurantAdmin, N'Demo Restaurant Manager', N'Restaurant operations account.', N'45208', 2, '2026-04-26T12:14:00Z', '2026-04-26T12:14:00Z')
) AS source (UserId, DisplayName, Bio, HomeAreaZipCode, SocialGoal, CreatedAtUtc, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (UserId, DisplayName, Bio, HomeAreaZipCode, SocialGoal, CreatedAtUtc, UpdatedAtUtc)
VALUES (source.UserId, source.DisplayName, source.Bio, source.HomeAreaZipCode, source.SocialGoal, source.CreatedAtUtc, source.UpdatedAtUtc);

MERGE dbo.UserPreferences AS target
USING (VALUES (@Alex, 1, '2026-04-26T12:20:00Z'), (@Brooke, 2, '2026-04-26T12:21:00Z'), (@Mod, 1, '2026-04-26T12:22:00Z'), (@Admin, 1, '2026-04-26T12:23:00Z'), (@RestaurantAdmin, 0, '2026-04-26T12:24:00Z')) AS source (UserId, SpiceTolerance, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (UserId, SpiceTolerance, UpdatedAtUtc) VALUES (source.UserId, source.SpiceTolerance, source.UpdatedAtUtc);

MERGE dbo.UserCuisinePreferences AS target
USING (VALUES
    (@Alex, CONVERT(UNIQUEIDENTIFIER, '10000000-0000-0000-0000-000000000001')),
    (@Alex, CONVERT(UNIQUEIDENTIFIER, '10000000-0000-0000-0000-000000000002')),
    (@Brooke, CONVERT(UNIQUEIDENTIFIER, '10000000-0000-0000-0000-000000000012')),
    (@RestaurantAdmin, CONVERT(UNIQUEIDENTIFIER, '10000000-0000-0000-0000-000000000011'))
) AS source (UserId, CuisineId)
ON target.UserId = source.UserId AND target.CuisineId = source.CuisineId
WHEN NOT MATCHED THEN INSERT (UserId, CuisineId) VALUES (source.UserId, source.CuisineId);

MERGE dbo.UserDietaryFlags AS target
USING (VALUES (@Brooke, N'Vegetarian'), (@Alex, N'Gluten-Aware')) AS source (UserId, DietaryFlag)
ON target.UserId = source.UserId AND target.DietaryFlag = source.DietaryFlag
WHEN NOT MATCHED THEN INSERT (UserId, DietaryFlag) VALUES (source.UserId, source.DietaryFlag);

MERGE dbo.UserAllergies AS target
USING (VALUES (@Alex, N'Peanuts'), (@Brooke, N'Shellfish')) AS source (UserId, Allergy)
ON target.UserId = source.UserId AND target.Allergy = source.Allergy
WHEN NOT MATCHED THEN INSERT (UserId, Allergy) VALUES (source.UserId, source.Allergy);

MERGE dbo.PrivacySettings AS target
USING (VALUES (@Alex, 1, '2026-04-26T12:30:00Z'), (@Brooke, 1, '2026-04-26T12:31:00Z'), (@Mod, 1, '2026-04-26T12:32:00Z'), (@Admin, 1, '2026-04-26T12:33:00Z'), (@RestaurantAdmin, 1, '2026-04-26T12:34:00Z')) AS source (UserId, DiscoveryEnabled, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (UserId, DiscoveryEnabled, UpdatedAtUtc) VALUES (source.UserId, source.DiscoveryEnabled, source.UpdatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.PasswordResetRequests WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000011001'))
    INSERT INTO dbo.PasswordResetRequests (Id, Username, Message, MatchedUserId, CreatedAtUtc, ClosedAtUtc, ClosedByUserId)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000011001'), N'tb_demo_alex', N'Demo reset request for admin review.', @Alex, '2026-04-26T13:00:00Z', NULL, NULL);

MERGE dbo.RecurringAvailabilityWindows AS target
USING (VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000001001'), @Alex, 5, '18:00:00', '21:00:00', N'Friday dinner', '2026-04-26T13:05:00Z', '2026-04-26T13:05:00Z')) AS source (Id, UserId, DayOfWeek, StartTime, EndTime, Label, CreatedAtUtc, UpdatedAtUtc)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, UserId, DayOfWeek, StartTime, EndTime, Label, CreatedAtUtc, UpdatedAtUtc) VALUES (source.Id, source.UserId, source.DayOfWeek, source.StartTime, source.EndTime, source.Label, source.CreatedAtUtc, source.UpdatedAtUtc);

MERGE dbo.OneOffAvailabilityWindows AS target
USING (VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000001101'), @Brooke, '2026-05-02T18:00:00Z', '2026-05-02T22:00:00Z', N'Demo dinner window', '2026-04-26T13:06:00Z', '2026-04-26T13:06:00Z')) AS source (Id, UserId, StartsAtUtc, EndsAtUtc, Label, CreatedAtUtc, UpdatedAtUtc)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, UserId, StartsAtUtc, EndsAtUtc, Label, CreatedAtUtc, UpdatedAtUtc) VALUES (source.Id, source.UserId, source.StartsAtUtc, source.EndsAtUtc, source.Label, source.CreatedAtUtc, source.UpdatedAtUtc);

MERGE dbo.SwipeDecisions AS target
USING (VALUES (@Alex, @Brooke, 0, '2026-04-26T13:10:00Z'), (@Brooke, @Alex, 0, '2026-04-26T13:11:00Z')) AS source (ActorUserId, SubjectUserId, Decision, UpdatedAtUtc)
ON target.ActorUserId = source.ActorUserId AND target.SubjectUserId = source.SubjectUserId
WHEN NOT MATCHED THEN INSERT (ActorUserId, SubjectUserId, Decision, UpdatedAtUtc) VALUES (source.ActorUserId, source.SubjectUserId, source.Decision, source.UpdatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.BudConnections WHERE Id = @BudConnection)
    INSERT INTO dbo.BudConnections (Id, UserOneId, UserTwoId, State, CreatedAtUtc, EndedAtUtc)
    VALUES (@BudConnection, @Alex, @Brooke, 0, '2026-04-26T13:12:00Z', NULL);

MERGE dbo.UserBlocks AS target
USING (VALUES (@Brooke, @RestaurantAdmin, '2026-04-26T13:13:00Z')) AS source (BlockerUserId, BlockedUserId, CreatedAtUtc)
ON target.BlockerUserId = source.BlockerUserId AND target.BlockedUserId = source.BlockedUserId
WHEN NOT MATCHED THEN INSERT (BlockerUserId, BlockedUserId, CreatedAtUtc) VALUES (source.BlockerUserId, source.BlockedUserId, source.CreatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.RestaurantAdminAssignments WHERE RestaurantId = CONVERT(UNIQUEIDENTIFIER, '77777777-7777-7777-7777-777777777777') AND UserId = @RestaurantAdmin)
    INSERT INTO dbo.RestaurantAdminAssignments (RestaurantId, UserId, CreatedAtUtc, RevokedAtUtc)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '77777777-7777-7777-7777-777777777777'), @RestaurantAdmin, '2026-04-26T13:20:00Z', NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE Id = @PublicGroup)
    INSERT INTO dbo.Groups (Id, OwnerUserId, Name, Description, Visibility, LifecycleState, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@PublicGroup, @Alex, N'Demo Supper Club', N'Public group seeded for feature coverage.', 0, 0, '2026-04-26T13:25:00Z', '2026-04-26T13:25:00Z');
IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE Id = @PrivateGroup)
    INSERT INTO dbo.Groups (Id, OwnerUserId, Name, Description, Visibility, LifecycleState, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@PrivateGroup, @Mod, N'Demo Safety Review', N'Private group seeded for invite coverage.', 1, 0, '2026-04-26T13:26:00Z', '2026-04-26T13:26:00Z');

MERGE dbo.GroupMembers AS target
USING (VALUES (@PublicGroup, @Alex, 0, '2026-04-26T13:25:00Z', '2026-04-26T13:25:00Z'), (@PublicGroup, @Brooke, 0, '2026-04-26T13:27:00Z', '2026-04-26T13:27:00Z'), (@PrivateGroup, @Mod, 0, '2026-04-26T13:26:00Z', '2026-04-26T13:26:00Z'), (@PrivateGroup, @Admin, 0, '2026-04-26T13:28:00Z', '2026-04-26T13:28:00Z')) AS source (GroupId, UserId, State, CreatedAtUtc, UpdatedAtUtc)
ON target.GroupId = source.GroupId AND target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (GroupId, UserId, State, CreatedAtUtc, UpdatedAtUtc) VALUES (source.GroupId, source.UserId, source.State, source.CreatedAtUtc, source.UpdatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.GroupInvites WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002101'))
    INSERT INTO dbo.GroupInvites (Id, GroupId, InvitedUserId, InviterUserId, Status, CreatedAtUtc, UpdatedAtUtc)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002101'), @PrivateGroup, @Brooke, @Mod, 0, '2026-04-26T13:30:00Z', '2026-04-26T13:30:00Z');

IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Id = @OpenEvent)
    INSERT INTO dbo.Events (Id, HostUserId, Title, EventType, Status, EventStartAtUtc, DecisionAtUtc, Capacity, MinParticipantsToRun, SelectedRestaurantId, CuisineTarget, GroupId, CancellationReason, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CompletedAtUtc)
    VALUES (@OpenEvent, @Alex, N'Demo Pizza Table', 0, 0, '2026-05-06T02:00:00Z', '2026-05-06T01:00:00Z', 3, 2, CONVERT(UNIQUEIDENTIFIER, '88888888-8888-8888-8888-888888888888'), NULL, @PublicGroup, NULL, '2026-04-26T14:00:00Z', '2026-04-26T14:00:00Z', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Id = @CompletedEvent)
    INSERT INTO dbo.Events (Id, HostUserId, Title, EventType, Status, EventStartAtUtc, DecisionAtUtc, Capacity, MinParticipantsToRun, SelectedRestaurantId, CuisineTarget, GroupId, CancellationReason, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CompletedAtUtc)
    VALUES (@CompletedEvent, @Brooke, N'Demo Completed Brunch', 0, 4, '2026-04-20T15:30:00Z', '2026-04-20T14:00:00Z', 4, 2, CONVERT(UNIQUEIDENTIFIER, '77777777-7777-7777-7777-777777777777'), NULL, @PublicGroup, NULL, '2026-04-19T14:00:00Z', '2026-04-20T17:00:00Z', NULL, '2026-04-20T17:00:00Z');

MERGE dbo.EventParticipants AS target
USING (VALUES (@OpenEvent, @Alex, 1, NULL, '2026-04-26T14:00:00Z', '2026-04-26T14:00:00Z', NULL, NULL), (@OpenEvent, @Brooke, 1, NULL, '2026-04-26T14:10:00Z', '2026-04-26T14:10:00Z', NULL, NULL), (@OpenEvent, @RestaurantAdmin, 1, NULL, '2026-04-26T14:15:00Z', '2026-04-26T14:15:00Z', NULL, NULL), (@CompletedEvent, @Brooke, 1, NULL, '2026-04-19T14:00:00Z', '2026-04-19T14:00:00Z', NULL, NULL), (@CompletedEvent, @Alex, 1, NULL, '2026-04-19T14:10:00Z', '2026-04-19T14:10:00Z', NULL, NULL)) AS source (EventId, UserId, State, InvitedAtUtc, JoinedAtUtc, RespondedAtUtc, LeftAtUtc, RemovedAtUtc)
ON target.EventId = source.EventId AND target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (EventId, UserId, State, InvitedAtUtc, JoinedAtUtc, RespondedAtUtc, LeftAtUtc, RemovedAtUtc) VALUES (source.EventId, source.UserId, source.State, source.InvitedAtUtc, source.JoinedAtUtc, source.RespondedAtUtc, source.LeftAtUtc, source.RemovedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.EventFeedbacks WHERE Id = @Feedback)
    INSERT INTO dbo.EventFeedbacks (Id, EventId, AuthorUserId, Rating, Text, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@Feedback, @CompletedEvent, @Alex, 5, N'Demo completed event feedback with a photo.', '2026-04-21T12:00:00Z', '2026-04-21T12:00:00Z');

IF NOT EXISTS (SELECT 1 FROM dbo.GroupAnnouncements WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002901'))
    INSERT INTO dbo.GroupAnnouncements (Id, GroupId, AuthorUserId, AnnouncementType, Title, Body, RelatedEventId, CreatedAtUtc)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000002901'), @PublicGroup, @Alex, 1, N'Demo event posted', N'This seeded group links to a live event.', @OpenEvent, '2026-04-26T14:20:00Z');

IF NOT EXISTS (SELECT 1 FROM dbo.RestaurantSlots WHERE Id = @Slot)
    INSERT INTO dbo.RestaurantSlots (Id, RestaurantId, StartsAtUtc, EndsAtUtc, Capacity, CutoffAtUtc, MinThresholdForDiscount, DiscountPercent, Status, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CancellationReason)
    VALUES (@Slot, CONVERT(UNIQUEIDENTIFIER, '88888888-8888-8888-8888-888888888888'), '2026-05-06T01:30:00Z', '2026-05-06T03:30:00Z', 3, '2026-05-06T01:00:00Z', 3, 10, 0, '2026-04-26T14:25:00Z', '2026-04-26T14:25:00Z', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EventSlotReservations WHERE Id = @Reservation)
    INSERT INTO dbo.EventSlotReservations (Id, EventId, SlotId, Status, CreatedAtUtc, CancelledAtUtc, CancellationReason)
    VALUES (@Reservation, @OpenEvent, @Slot, 0, '2026-04-26T14:26:00Z', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.DiscountActivations WHERE ReservationId = @Reservation)
    INSERT INTO dbo.DiscountActivations (ReservationId, IsActive, IsFinalized, EvaluatedAtUtc)
    VALUES (@Reservation, 1, 0, '2026-04-26T14:27:00Z');
IF NOT EXISTS (SELECT 1 FROM dbo.CheckoutSessions WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000012001'))
    INSERT INTO dbo.CheckoutSessions (Id, EventId, UserId, Status, Currency, SubtotalCents, DiscountCents, TotalCents, CreatedAtUtc, UpdatedAtUtc, CompletedAtUtc, CancelledAtUtc)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000012001'), @OpenEvent, @Alex, 0, N'USD', 1500, 150, 1350, '2026-04-26T14:28:00Z', '2026-04-26T14:28:00Z', NULL, NULL);

MERGE dbo.ChatThreads AS target
USING (VALUES (@EventThread, 0, @OpenEvent, '2026-04-26T14:30:00Z'), (@GroupThread, 1, @PublicGroup, '2026-04-26T14:31:00Z'), (@DirectThread, 2, @BudConnection, '2026-04-26T14:32:00Z'), (@SupportThread, 3, @Alex, '2026-04-26T14:33:00Z')) AS source (Id, ScopeType, ScopeId, CreatedAtUtc)
ON target.ScopeType = source.ScopeType AND target.ScopeId = source.ScopeId
WHEN NOT MATCHED THEN INSERT (Id, ScopeType, ScopeId, CreatedAtUtc) VALUES (source.Id, source.ScopeType, source.ScopeId, source.CreatedAtUtc);

MERGE dbo.ChatMessages AS target
USING (VALUES
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005001'), @EventThread, @Alex, N'Event chat is ready for the seeded pizza table.', '2026-04-26T14:35:00Z'),
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005002'), @GroupThread, @Brooke, N'Group chat is ready too.', '2026-04-26T14:36:00Z'),
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005003'), @DirectThread, @Alex, N'Direct chat remains seeded but feature-flagged.', '2026-04-26T14:37:00Z'),
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005004'), @SupportThread, @Alex, N'I need admin help before the demo.', '2026-04-26T14:38:00Z')
) AS source (Id, ThreadId, SenderUserId, Body, CreatedAtUtc)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, ThreadId, SenderUserId, Body, CreatedAtUtc) VALUES (source.Id, source.ThreadId, source.SenderUserId, source.Body, source.CreatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.Notifications WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000006001'))
    INSERT INTO dbo.Notifications (Id, RecipientUserId, NotificationType, ContextType, ContextId, Message, CreatedAtUtc, ReadAtUtc)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000006001'), @Brooke, 7, N'BudConnection', @BudConnection, N'You and Demo Alex are now Budz.', '2026-04-26T14:40:00Z', NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ModerationReports WHERE Id = @Report)
    INSERT INTO dbo.ModerationReports (Id, ReporterUserId, TargetType, TargetId, Category, Reason, Explanation, RelatedEventId, RelatedUserId, RelatedMessageId, Status, ResolvedByUserId, ResolvedAtUtc, ResolutionDecision, ResolutionNotes, CreatedAtUtc)
    VALUES (@Report, @Alex, 1, CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005004'), N'Chat', N'Demo support message needs review.', N'Pending report for admin detail rendering.', @OpenEvent, @Brooke, CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005004'), 0, NULL, NULL, NULL, NULL, '2026-04-26T14:45:00Z');
IF NOT EXISTS (SELECT 1 FROM dbo.ModerationActions WHERE Id = @Action)
    INSERT INTO dbo.ModerationActions (Id, ActorUserId, ReportId, ActionType, Notes, CreatedAtUtc)
    VALUES (@Action, @Mod, @Report, 1, N'Demo restriction action.', '2026-04-26T14:46:00Z');
IF NOT EXISTS (SELECT 1 FROM dbo.UserRestrictions WHERE Id = @Restriction)
    INSERT INTO dbo.UserRestrictions (Id, SubjectUserId, IssuedByUserId, ModerationActionId, Scope, Reason, StartsAtUtc, ExpiresAtUtc, Status, RevokedAtUtc)
    VALUES (@Restriction, @Brooke, @Mod, @Action, 0, N'Demo discovery restriction.', '2026-04-26T14:47:00Z', '2026-05-03T14:47:00Z', 0, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogEntries WHERE Id = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000008201'))
    INSERT INTO dbo.AuditLogEntries (Id, ActionType, ActorUserId, TargetEntityType, TargetEntityId, CreatedAtUtc, Details)
    VALUES (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000008201'), N'RestrictionCreated', @Mod, N'UserRestriction', @Restriction, '2026-04-26T14:48:00Z', N'{"source":"demo-feature-topup"}');

MERGE dbo.MediaAssets AS target
USING (VALUES
    (@AvatarMedia, @Alex, @Alex, NULL, NULL, NULL, N'tb-demo-avatar.png', N'image/png', 8, 0x89504E470D0A1A0A, '2026-04-26T14:50:00Z'),
    (@ReportMedia, @Alex, NULL, NULL, NULL, @Report, N'tb-demo-report.png', N'image/png', 8, 0x89504E470D0A1A0A, '2026-04-26T14:51:00Z'),
    (@FeedbackMedia, @Alex, NULL, NULL, @CompletedEvent, NULL, N'tb-demo-feedback.png', N'image/png', DATALENGTH(@DemoFeedbackPng), @DemoFeedbackPng, '2026-04-26T14:52:00Z')
) AS source (Id, OwnerUserId, ProfileUserId, GroupId, EventId, ReportId, OriginalFileName, ContentType, ContentLength, Content, CreatedAtUtc)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, OwnerUserId, ProfileUserId, GroupId, EventId, ReportId, OriginalFileName, ContentType, ContentLength, Content, CreatedAtUtc)
VALUES (source.Id, source.OwnerUserId, source.ProfileUserId, source.GroupId, source.EventId, source.ReportId, source.OriginalFileName, source.ContentType, source.ContentLength, source.Content, source.CreatedAtUtc);

UPDATE dbo.MediaAssets
SET ContentLength = DATALENGTH(@DemoFeedbackPng),
    Content = @DemoFeedbackPng,
    OwnerUserId = @Alex
WHERE Id = @FeedbackMedia
  AND ContentType = N'image/png'
  AND ContentLength = 8
  AND Content = 0x89504E470D0A1A0A;

UPDATE dbo.MediaAssets
SET OwnerUserId = @Alex
WHERE Id = @FeedbackMedia
  AND OwnerUserId = @Brooke;

IF NOT EXISTS (SELECT 1 FROM dbo.EventFeedbackPhotos WHERE EventFeedbackId = @Feedback AND MediaAssetId = @FeedbackMedia)
    INSERT INTO dbo.EventFeedbackPhotos (EventFeedbackId, MediaAssetId, CreatedAtUtc)
    VALUES (@Feedback, @FeedbackMedia, '2026-04-26T14:53:00Z');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260426-demo-feature-topup')
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260426-demo-feature-topup', N'Minimal optional demo feature data top-up with rollback.');

COMMIT TRANSACTION;
