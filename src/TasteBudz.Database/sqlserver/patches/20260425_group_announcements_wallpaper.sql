SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Groups', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Groups', N'WallpaperTheme') IS NULL
BEGIN
    ALTER TABLE dbo.Groups
        ADD WallpaperTheme INT NOT NULL CONSTRAINT DF_Groups_WallpaperTheme DEFAULT 0 WITH VALUES;
END;
GO

IF OBJECT_ID(N'dbo.GroupAnnouncements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GroupAnnouncements (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_GroupAnnouncements PRIMARY KEY,
        GroupId UNIQUEIDENTIFIER NOT NULL,
        AuthorUserId UNIQUEIDENTIFIER NOT NULL,
        AnnouncementType INT NOT NULL,
        Title NVARCHAR(120) NOT NULL,
        Body NVARCHAR(1000) NOT NULL,
        RelatedEventId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_GroupAnnouncements_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups (Id),
        CONSTRAINT FK_GroupAnnouncements_UserAccounts_AuthorUserId FOREIGN KEY (AuthorUserId) REFERENCES dbo.UserAccounts (Id),
        CONSTRAINT FK_GroupAnnouncements_Events_RelatedEventId FOREIGN KEY (RelatedEventId) REFERENCES dbo.Events (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GroupAnnouncements_GroupId_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.GroupAnnouncements'))
    CREATE INDEX IX_GroupAnnouncements_GroupId_CreatedAtUtc ON dbo.GroupAnnouncements (GroupId, CreatedAtUtc);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'20260425-group-announcements-wallpaper')
BEGIN
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'20260425-group-announcements-wallpaper', N'Add group announcements and owner-selected wallpaper theme storage.');
END;
GO
