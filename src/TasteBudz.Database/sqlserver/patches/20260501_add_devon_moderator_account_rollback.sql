SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @ModeratorUserId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '00000000-0000-0000-0000-000000000104');
DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

DELETE FROM dbo.UserRoles
WHERE UserId = @ModeratorUserId
  AND Role IN (0, 1);

UPDATE dbo.UserSessions
SET RevokedAtUtc = COALESCE(RevokedAtUtc, @Now)
WHERE UserId = @ModeratorUserId;

UPDATE dbo.UserAccounts
SET Status = 1,
    DeletedAtUtc = COALESCE(DeletedAtUtc, @Now),
    UpdatedAtUtc = @Now
WHERE Id = @ModeratorUserId
  AND NormalizedUsername = N'DEVON'
  AND NormalizedEmail = N'DEVON@TASTEBUDZ.LOCAL';

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260501-add-devon-moderator-account-rollback')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260501-add-devon-moderator-account-rollback', N'Soft-delete the Devon moderator account and revoke its roles.');
END;

COMMIT TRANSACTION;
