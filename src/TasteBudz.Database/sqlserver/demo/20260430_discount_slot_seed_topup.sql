-- Optional Azure SQL discount-slot top-up for capstone demos.
-- Run only after the normal SQL Server schema, reference seed, current patches,
-- and 20260426_feature_seed_topup.sql are applied.
-- This script is guarded by fixed GUIDs and is intentionally separate from production bootstrap.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @Alex UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000101');
DECLARE @Mod UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000103');
DECLARE @Admin UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000104');
DECLARE @RestaurantAdmin UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000000105');
DECLARE @TacosRestaurant UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '33333333-3333-3333-3333-333333333333');
DECLARE @NoodlesRestaurant UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '44444444-4444-4444-4444-444444444444');
DECLARE @NoodlesEvent UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000003022');
DECLARE @TacosEvent UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000003023');
DECLARE @TacosOpenSlot UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009009');
DECLARE @NoodlesOpenSlot UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009010');
DECLARE @NoodlesReservedSlot UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009011');
DECLARE @TacosReservedSlot UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009012');
DECLARE @NoodlesReservation UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009105');
DECLARE @TacosReservation UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000009106');
DECLARE @NoodlesThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004015');
DECLARE @TacosThread UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000004016');

IF (SELECT COUNT(*) FROM dbo.UserAccounts WHERE Id IN (@Alex, @Mod, @Admin, @RestaurantAdmin)) <> 4
BEGIN
    THROW 51000, 'Required Azure demo users are missing. Apply 20260426_feature_seed_topup.sql before this discount-slot top-up.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Restaurants WHERE Id = @TacosRestaurant)
   OR NOT EXISTS (SELECT 1 FROM dbo.Restaurants WHERE Id = @NoodlesRestaurant)
BEGIN
    THROW 51001, 'Required seeded restaurants are missing. Apply the SQL Server reference seed before this discount-slot top-up.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Id = @NoodlesEvent)
    INSERT INTO dbo.Events (Id, HostUserId, Title, EventType, Status, EventStartAtUtc, DecisionAtUtc, Capacity, MinParticipantsToRun, SelectedRestaurantId, CuisineTarget, GroupId, CancellationReason, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CompletedAtUtc)
    VALUES (@NoodlesEvent, @Mod, N'Campus Noodles Discount Table', 0, 0, '2026-05-20T23:00:00Z', '2026-05-20T21:30:00Z', 5, 2, @NoodlesRestaurant, NULL, NULL, NULL, '2026-04-30T18:00:00Z', '2026-04-30T18:00:00Z', NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Id = @TacosEvent)
    INSERT INTO dbo.Events (Id, HostUserId, Title, EventType, Status, EventStartAtUtc, DecisionAtUtc, Capacity, MinParticipantsToRun, SelectedRestaurantId, CuisineTarget, GroupId, CancellationReason, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CompletedAtUtc)
    VALUES (@TacosEvent, @RestaurantAdmin, N'Over-the-Rhine Tacos Discount Table', 0, 0, '2026-05-21T22:30:00Z', '2026-05-21T21:00:00Z', 6, 2, @TacosRestaurant, NULL, NULL, NULL, '2026-04-30T18:05:00Z', '2026-04-30T18:05:00Z', NULL, NULL);

MERGE dbo.EventParticipants AS target
USING (VALUES
    (@NoodlesEvent, @Mod, 1, NULL, '2026-04-30T18:00:00Z', '2026-04-30T18:00:00Z', NULL, NULL),
    (@NoodlesEvent, @Alex, 1, NULL, '2026-04-30T18:02:00Z', '2026-04-30T18:02:00Z', NULL, NULL),
    (@NoodlesEvent, @Admin, 1, NULL, '2026-04-30T18:04:00Z', '2026-04-30T18:04:00Z', NULL, NULL),
    (@TacosEvent, @RestaurantAdmin, 1, NULL, '2026-04-30T18:05:00Z', '2026-04-30T18:05:00Z', NULL, NULL),
    (@TacosEvent, @Alex, 1, NULL, '2026-04-30T18:07:00Z', '2026-04-30T18:07:00Z', NULL, NULL)
) AS source (EventId, UserId, State, InvitedAtUtc, JoinedAtUtc, RespondedAtUtc, LeftAtUtc, RemovedAtUtc)
ON target.EventId = source.EventId AND target.UserId = source.UserId
WHEN NOT MATCHED THEN INSERT (EventId, UserId, State, InvitedAtUtc, JoinedAtUtc, RespondedAtUtc, LeftAtUtc, RemovedAtUtc)
VALUES (source.EventId, source.UserId, source.State, source.InvitedAtUtc, source.JoinedAtUtc, source.RespondedAtUtc, source.LeftAtUtc, source.RemovedAtUtc);

MERGE dbo.RestaurantSlots AS target
USING (VALUES
    (@TacosOpenSlot, @TacosRestaurant, '2026-05-18T22:30:00Z', '2026-05-19T00:30:00Z', 6, '2026-05-18T21:00:00Z', 4, 18, 0, '2026-04-30T18:10:00Z', '2026-04-30T18:10:00Z', NULL, NULL),
    (@NoodlesOpenSlot, @NoodlesRestaurant, '2026-05-19T23:00:00Z', '2026-05-20T01:00:00Z', 5, '2026-05-19T21:30:00Z', 3, 12, 0, '2026-04-30T18:15:00Z', '2026-04-30T18:15:00Z', NULL, NULL),
    (@NoodlesReservedSlot, @NoodlesRestaurant, '2026-05-20T23:00:00Z', '2026-05-21T01:00:00Z', 5, '2026-05-20T21:30:00Z', 3, 15, 0, '2026-04-30T18:20:00Z', '2026-04-30T18:20:00Z', NULL, NULL),
    (@TacosReservedSlot, @TacosRestaurant, '2026-05-21T22:00:00Z', '2026-05-22T00:00:00Z', 6, '2026-05-21T21:00:00Z', 4, 20, 0, '2026-04-30T18:25:00Z', '2026-04-30T18:25:00Z', NULL, NULL)
) AS source (Id, RestaurantId, StartsAtUtc, EndsAtUtc, Capacity, CutoffAtUtc, MinThresholdForDiscount, DiscountPercent, Status, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CancellationReason)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, RestaurantId, StartsAtUtc, EndsAtUtc, Capacity, CutoffAtUtc, MinThresholdForDiscount, DiscountPercent, Status, CreatedAtUtc, UpdatedAtUtc, CancelledAtUtc, CancellationReason)
VALUES (source.Id, source.RestaurantId, source.StartsAtUtc, source.EndsAtUtc, source.Capacity, source.CutoffAtUtc, source.MinThresholdForDiscount, source.DiscountPercent, source.Status, source.CreatedAtUtc, source.UpdatedAtUtc, source.CancelledAtUtc, source.CancellationReason);

IF NOT EXISTS (SELECT 1 FROM dbo.EventSlotReservations WHERE Id = @NoodlesReservation)
    INSERT INTO dbo.EventSlotReservations (Id, EventId, SlotId, Status, CreatedAtUtc, CancelledAtUtc, CancellationReason)
    VALUES (@NoodlesReservation, @NoodlesEvent, @NoodlesReservedSlot, 0, '2026-04-30T18:30:00Z', NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.EventSlotReservations WHERE Id = @TacosReservation)
    INSERT INTO dbo.EventSlotReservations (Id, EventId, SlotId, Status, CreatedAtUtc, CancelledAtUtc, CancellationReason)
    VALUES (@TacosReservation, @TacosEvent, @TacosReservedSlot, 0, '2026-04-30T18:35:00Z', NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.DiscountActivations WHERE ReservationId = @NoodlesReservation)
    INSERT INTO dbo.DiscountActivations (ReservationId, IsActive, IsFinalized, EvaluatedAtUtc)
    VALUES (@NoodlesReservation, 1, 0, '2026-04-30T18:40:00Z');

IF NOT EXISTS (SELECT 1 FROM dbo.DiscountActivations WHERE ReservationId = @TacosReservation)
    INSERT INTO dbo.DiscountActivations (ReservationId, IsActive, IsFinalized, EvaluatedAtUtc)
    VALUES (@TacosReservation, 0, 0, '2026-04-30T18:41:00Z');

MERGE dbo.ChatThreads AS target
USING (VALUES
    (@NoodlesThread, 0, @NoodlesEvent, '2026-04-30T18:00:00Z'),
    (@TacosThread, 0, @TacosEvent, '2026-04-30T18:05:00Z')
) AS source (Id, ScopeType, ScopeId, CreatedAtUtc)
ON target.ScopeType = source.ScopeType AND target.ScopeId = source.ScopeId
WHEN NOT MATCHED THEN INSERT (Id, ScopeType, ScopeId, CreatedAtUtc)
VALUES (source.Id, source.ScopeType, source.ScopeId, source.CreatedAtUtc);

MERGE dbo.ChatMessages AS target
USING (VALUES
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005017'), @NoodlesThread, @Mod, N'Campus Noodles has a discount if three of us stay confirmed.', '2026-04-30T18:01:00Z'),
    (CONVERT(UNIQUEIDENTIFIER, '20000000-0000-0000-0000-000000005018'), @TacosThread, @RestaurantAdmin, N'Taco discount is seeded but still needs a larger table to activate.', '2026-04-30T18:06:00Z')
) AS source (Id, ThreadId, SenderUserId, Body, CreatedAtUtc)
ON target.Id = source.Id
WHEN NOT MATCHED THEN INSERT (Id, ThreadId, SenderUserId, Body, CreatedAtUtc)
VALUES (source.Id, source.ThreadId, source.SenderUserId, source.Body, source.CreatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260430-demo-discount-slot-topup')
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260430-demo-discount-slot-topup', N'Additional optional demo discount slots and slot-linked events.');

COMMIT TRANSACTION;
