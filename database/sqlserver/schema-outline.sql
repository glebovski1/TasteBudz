-- Draft only: this file captures the baseline SQL table intent without committing to a runtime migration tool.
-- Do not treat this as the final physical schema.

-- Identity and profile tables
-- UserAccounts
-- UserProfiles
-- UserPreferences
-- RecurringAvailabilityWindows
-- OneOffAvailabilityWindows
-- PrivacySettings
-- UserBlocks

-- Restaurants and events tables
-- Restaurants
-- Events
-- EventParticipants

-- Groups and discovery tables
-- Groups
-- GroupMembers
-- GroupInvites
-- SwipeDecisions
-- BudConnections

-- Supporting tables
-- Notifications
-- ModerationReports
-- ModerationActions
-- UserRestrictions
-- AuditLogEntries

-- Baseline constraints to preserve:
-- * unique EventParticipants(EventId, UserId)
-- * unique normalized BudConnections pair
-- * unique GroupMembers(GroupId, UserId)
-- * unique UserBlocks(BlockerUserId, BlockedUserId)
-- * Event Capacity between 2 and 8
-- * exactly one of SelectedRestaurantId or CuisineTarget on Events
