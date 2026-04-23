SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.PasswordResetRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetRequests (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordResetRequests PRIMARY KEY,
        Username NVARCHAR(80) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        MatchedUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        ClosedAtUtc DATETIMEOFFSET NULL,
        ClosedByUserId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_PasswordResetRequests_UserAccounts_MatchedUserId FOREIGN KEY (MatchedUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_PasswordResetRequests_UserAccounts_ClosedByUserId FOREIGN KEY (ClosedByUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT CK_PasswordResetRequests_ClosedPair CHECK (
            (ClosedAtUtc IS NULL AND ClosedByUserId IS NULL) OR
            (ClosedAtUtc IS NOT NULL AND ClosedByUserId IS NOT NULL)
        ),
        CONSTRAINT CK_PasswordResetRequests_ClosedAtUtc CHECK (
            ClosedAtUtc IS NULL OR ClosedAtUtc >= CreatedAtUtc
        )
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetRequests_ClosedAtUtc_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.PasswordResetRequests'))
    CREATE INDEX IX_PasswordResetRequests_ClosedAtUtc_CreatedAtUtc ON dbo.PasswordResetRequests (ClosedAtUtc, CreatedAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetRequests_MatchedUserId' AND object_id = OBJECT_ID(N'dbo.PasswordResetRequests'))
    CREATE INDEX IX_PasswordResetRequests_MatchedUserId ON dbo.PasswordResetRequests (MatchedUserId);
GO

IF OBJECT_ID(N'dbo.Restaurants', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Restaurants', N'StreetAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Restaurants
        ADD StreetAddress NVARCHAR(160) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Restaurants', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Restaurants', N'IsArchived') IS NULL
BEGIN
    ALTER TABLE dbo.Restaurants
        ADD IsArchived BIT NOT NULL CONSTRAINT DF_Restaurants_IsArchived DEFAULT 0 WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Restaurants_IsArchived' AND object_id = OBJECT_ID(N'dbo.Restaurants'))
    CREATE INDEX IX_Restaurants_IsArchived ON dbo.Restaurants (IsArchived);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260422-password-reset-requests-and-restaurant-catalog')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260422-password-reset-requests-and-restaurant-catalog', N'Add password reset requests plus restaurant archive and street address fields.');
END;
GO
