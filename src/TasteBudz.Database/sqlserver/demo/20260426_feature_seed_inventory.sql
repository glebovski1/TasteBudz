-- Optional Azure SQL demo-readiness inventory.
-- This script is read-only and is not part of normal production bootstrap.

SET NOCOUNT ON;

SELECT N'User accounts' AS Feature, COUNT_BIG(*) AS RowCount FROM dbo.UserAccounts
UNION ALL SELECT N'Roles', COUNT_BIG(*) FROM dbo.UserRoles
UNION ALL SELECT N'Password reset requests', COUNT_BIG(*) FROM dbo.PasswordResetRequests WHERE ClosedAtUtc IS NULL
UNION ALL SELECT N'Profiles', COUNT_BIG(*) FROM dbo.UserProfiles
UNION ALL SELECT N'Cuisine preferences', COUNT_BIG(*) FROM dbo.UserCuisinePreferences
UNION ALL SELECT N'Dietary flags', COUNT_BIG(*) FROM dbo.UserDietaryFlags
UNION ALL SELECT N'Allergies', COUNT_BIG(*) FROM dbo.UserAllergies
UNION ALL SELECT N'Recurring availability', COUNT_BIG(*) FROM dbo.RecurringAvailabilityWindows
UNION ALL SELECT N'One-off availability', COUNT_BIG(*) FROM dbo.OneOffAvailabilityWindows
UNION ALL SELECT N'Swipe decisions', COUNT_BIG(*) FROM dbo.SwipeDecisions
UNION ALL SELECT N'Bud connections', COUNT_BIG(*) FROM dbo.BudConnections
UNION ALL SELECT N'Blocks', COUNT_BIG(*) FROM dbo.UserBlocks
UNION ALL SELECT N'Groups', COUNT_BIG(*) FROM dbo.Groups
UNION ALL SELECT N'Group members', COUNT_BIG(*) FROM dbo.GroupMembers
UNION ALL SELECT N'Group invites', COUNT_BIG(*) FROM dbo.GroupInvites
UNION ALL SELECT N'Group announcements', COUNT_BIG(*) FROM dbo.GroupAnnouncements
UNION ALL SELECT N'Restaurants', COUNT_BIG(*) FROM dbo.Restaurants WHERE IsArchived = 0
UNION ALL SELECT N'Restaurant admin assignments', COUNT_BIG(*) FROM dbo.RestaurantAdminAssignments WHERE RevokedAtUtc IS NULL
UNION ALL SELECT N'Restaurant slots', COUNT_BIG(*) FROM dbo.RestaurantSlots WHERE Status = 0
UNION ALL SELECT N'Event slot reservations', COUNT_BIG(*) FROM dbo.EventSlotReservations WHERE Status = 0
UNION ALL SELECT N'Discount activations', COUNT_BIG(*) FROM dbo.DiscountActivations WHERE IsActive = 1
UNION ALL SELECT N'Events', COUNT_BIG(*) FROM dbo.Events
UNION ALL SELECT N'Event participants', COUNT_BIG(*) FROM dbo.EventParticipants
UNION ALL SELECT N'Event feedback', COUNT_BIG(*) FROM dbo.EventFeedbacks
UNION ALL SELECT N'Event feedback photos', COUNT_BIG(*) FROM dbo.EventFeedbackPhotos
UNION ALL SELECT N'Checkout sessions', COUNT_BIG(*) FROM dbo.CheckoutSessions
UNION ALL SELECT N'Event chat threads', COUNT_BIG(*) FROM dbo.ChatThreads WHERE ScopeType = 0
UNION ALL SELECT N'Group chat threads', COUNT_BIG(*) FROM dbo.ChatThreads WHERE ScopeType = 1
UNION ALL SELECT N'Direct chat threads', COUNT_BIG(*) FROM dbo.ChatThreads WHERE ScopeType = 2
UNION ALL SELECT N'Support chat threads', COUNT_BIG(*) FROM dbo.ChatThreads WHERE ScopeType = 3
UNION ALL SELECT N'Chat messages', COUNT_BIG(*) FROM dbo.ChatMessages
UNION ALL SELECT N'Notifications', COUNT_BIG(*) FROM dbo.Notifications
UNION ALL SELECT N'Pending moderation reports', COUNT_BIG(*) FROM dbo.ModerationReports WHERE Status = 0
UNION ALL SELECT N'Moderation actions', COUNT_BIG(*) FROM dbo.ModerationActions
UNION ALL SELECT N'Active user restrictions', COUNT_BIG(*) FROM dbo.UserRestrictions WHERE Status = 0
UNION ALL SELECT N'Profile avatar media', COUNT_BIG(*) FROM dbo.MediaAssets WHERE ProfileUserId IS NOT NULL
UNION ALL SELECT N'Report evidence media', COUNT_BIG(*) FROM dbo.MediaAssets WHERE ReportId IS NOT NULL
UNION ALL SELECT N'Event feedback media', COUNT_BIG(*) FROM dbo.MediaAssets WHERE EventId IS NOT NULL;
