SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @ModeratorUserId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '00000000-0000-0000-0000-000000000104');
DECLARE @CuisineId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '10000000-0000-0000-0000-000000000006');
DECLARE @Username NVARCHAR(80) = N'devon';
DECLARE @NormalizedUsername NVARCHAR(80) = N'DEVON';
DECLARE @Email NVARCHAR(320) = N'devon@tastebudz.local';
DECLARE @NormalizedEmail NVARCHAR(320) = N'DEVON@TASTEBUDZ.LOCAL';
DECLARE @PasswordHash NVARCHAR(MAX) = N'v1.100000.MTIzNDU2Nzg5Ojs8PT4/QA==.yEQ/nu9SSFg94N80LauJBEIaPZaZhJTBt2cVTR4ratc=';
DECLARE @CreatedAtUtc DATETIMEOFFSET = '2026-03-04T12:00:00Z';
DECLARE @UpdatedAtUtc DATETIMEOFFSET = '2026-03-25T16:15:00Z';

IF EXISTS (
    SELECT 1
    FROM dbo.UserAccounts
    WHERE Id = @ModeratorUserId
      AND (NormalizedUsername <> @NormalizedUsername OR NormalizedEmail <> @NormalizedEmail)
)
BEGIN
    THROW 51000, 'The deterministic Devon moderator user id is already assigned to a different account.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM dbo.UserAccounts
    WHERE Id <> @ModeratorUserId
      AND (NormalizedUsername = @NormalizedUsername OR NormalizedEmail = @NormalizedEmail)
)
BEGIN
    THROW 51001, 'The Devon moderator username or email is already assigned to a different account.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Cuisines WHERE Id = @CuisineId)
BEGIN
    THROW 51002, 'Required cuisine reference data is missing for the Devon moderator account.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE Id = @ModeratorUserId)
BEGIN
    INSERT INTO dbo.UserAccounts (
        Id,
        Username,
        NormalizedUsername,
        Email,
        NormalizedEmail,
        PasswordHash,
        Status,
        CreatedAtUtc,
        UpdatedAtUtc,
        DeletedAtUtc
    )
    VALUES (
        @ModeratorUserId,
        @Username,
        @NormalizedUsername,
        @Email,
        @NormalizedEmail,
        @PasswordHash,
        0,
        @CreatedAtUtc,
        @UpdatedAtUtc,
        NULL
    );
END
ELSE
BEGIN
    UPDATE dbo.UserAccounts
    SET Status = 0,
        DeletedAtUtc = NULL,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Id = @ModeratorUserId
      AND (Status <> 0 OR DeletedAtUtc IS NOT NULL);
END;

MERGE dbo.UserRoles AS target
USING (VALUES (@ModeratorUserId, 0), (@ModeratorUserId, 1)) AS source (UserId, Role)
ON target.UserId = source.UserId AND target.Role = source.Role
WHEN NOT MATCHED THEN
    INSERT (UserId, Role) VALUES (source.UserId, source.Role);

MERGE dbo.UserProfiles AS target
USING (VALUES (
    @ModeratorUserId,
    N'Devon Brooks',
    N'Moderator account for support and safety testing.',
    N'45219',
    2,
    '2026-03-04T12:05:00Z',
    @UpdatedAtUtc
)) AS source (UserId, DisplayName, Bio, HomeAreaZipCode, SocialGoal, CreatedAtUtc, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN
    INSERT (UserId, DisplayName, Bio, HomeAreaZipCode, SocialGoal, CreatedAtUtc, UpdatedAtUtc)
    VALUES (source.UserId, source.DisplayName, source.Bio, source.HomeAreaZipCode, source.SocialGoal, source.CreatedAtUtc, source.UpdatedAtUtc);

MERGE dbo.UserPreferences AS target
USING (VALUES (@ModeratorUserId, 1, @UpdatedAtUtc)) AS source (UserId, SpiceTolerance, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN
    INSERT (UserId, SpiceTolerance, UpdatedAtUtc)
    VALUES (source.UserId, source.SpiceTolerance, source.UpdatedAtUtc);

MERGE dbo.UserCuisinePreferences AS target
USING (VALUES (@ModeratorUserId, @CuisineId)) AS source (UserId, CuisineId)
ON target.UserId = source.UserId AND target.CuisineId = source.CuisineId
WHEN NOT MATCHED THEN
    INSERT (UserId, CuisineId) VALUES (source.UserId, source.CuisineId);

MERGE dbo.PrivacySettings AS target
USING (VALUES (@ModeratorUserId, CONVERT(BIT, 1), @UpdatedAtUtc)) AS source (UserId, DiscoveryEnabled, UpdatedAtUtc)
ON target.UserId = source.UserId
WHEN NOT MATCHED THEN
    INSERT (UserId, DiscoveryEnabled, UpdatedAtUtc)
    VALUES (source.UserId, source.DiscoveryEnabled, source.UpdatedAtUtc);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260501-add-devon-moderator-account')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260501-add-devon-moderator-account', N'Add the Devon moderator account for Azure moderation testing.');
END;

COMMIT TRANSACTION;
