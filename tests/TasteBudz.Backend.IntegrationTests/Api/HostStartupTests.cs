// Basic integration smoke tests for host startup and routing.
using System.Buffers.Binary;
using System.Net;
using Microsoft.Data.Sqlite;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.IntegrationTests.Shared;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Confirms the test host boots and still returns the framework-standard 404 for unknown routes.
/// </summary>
public sealed class HostStartupTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task MissingRoute_Returns404()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingRuntimeTable_WithInitializationDisabled_FailsFastOnStartup()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:InitializeSqliteOnStartup"] = "false",
        });

        await SqliteDatabaseBootstrapper.RecreateDatabaseAsync(customFactory.ConnectionString);
        await DropTableAsync(customFactory.ConnectionString, "UserRoles");

        var exception = await Record.ExceptionAsync(async () =>
        {
            using var client = customFactory.CreateClient();
            await client.GetAsync("/definitely-missing");
        });

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("UserRoles", invalidOperation.Message);
    }

    [Fact]
    public async Task OldRuntimeTableShape_WithInitializationDisabled_FailsFastOnStartup()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:InitializeSqliteOnStartup"] = "false",
        });

        await SqliteDatabaseBootstrapper.RecreateDatabaseAsync(customFactory.ConnectionString);
        await DropTableAsync(customFactory.ConnectionString, "MediaAssets");
        await CreateOldMediaAssetsTableAsync(customFactory.ConnectionString);

        var exception = await Record.ExceptionAsync(async () =>
        {
            using var client = customFactory.CreateClient();
            await client.GetAsync("/definitely-missing");
        });

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("MediaAssets", invalidOperation.Message);
        Assert.Contains("missing required column", invalidOperation.Message);
    }

    [Fact]
    public async Task OldRestaurantSlotsTable_WithInitializationEnabled_AddsDiscountPercentOnStartup()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:InitializeSqliteOnStartup"] = "true",
            ["Persistence:SeedTestDataOnStartup"] = "false",
        });

        await CreateOldRestaurantSlotsTableWithoutDiscountPercentAsync(customFactory.ConnectionString);

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("DiscountPercent", await ListColumnsAsync(customFactory.ConnectionString, "RestaurantSlots"));
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenEnabled_PopulatesDevelopmentAccounts()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var connection = new SqliteConnection(customFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM UserAccounts WHERE Username = 'alex';";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenEnabled_CoversImplementedFeatureSurfaces()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserAccounts") >= 10);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserRoles", "Role = 1") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserRoles", "Role = 2") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserRoles", "Role = 3") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "PasswordResetRequests", "ClosedAtUtc IS NULL") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserProfiles") >= 10);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserCuisinePreferences") >= 10);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserDietaryFlags") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserAllergies") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "RecurringAvailabilityWindows") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "OneOffAvailabilityWindows") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "SwipeDecisions") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "BudConnections") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserBlocks") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "Groups") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "GroupMembers") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "GroupInvites") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "GroupAnnouncements") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "Restaurants") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "RestaurantAdminAssignments", "RevokedAtUtc IS NULL") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "RestaurantSlots", "Status = 0") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "EventSlotReservations", "Status = 0") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "DiscountActivations", "IsActive = 1") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "Events") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "EventParticipants") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "EventFeedbacks") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "EventFeedbackPhotos") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "CheckoutSessions") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatThreads", "ScopeType = 0") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatThreads", "ScopeType = 1") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatThreads", "ScopeType = 2") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatThreads", "ScopeType = 3") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatMessages") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "Notifications") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ModerationReports", "Status = 0") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ModerationActions") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserRestrictions", "Status = 0") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "MediaAssets", "ProfileUserId IS NOT NULL") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "MediaAssets", "ReportId IS NOT NULL") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "MediaAssets", "EventId IS NOT NULL") >= 1);
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenEnabled_StoresRenderableFeedbackPhotoBytes()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var connection = new SqliteConnection(customFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT m.ContentType, m.ContentLength, m.Content
            FROM MediaAssets m
            INNER JOIN EventFeedbackPhotos p ON p.MediaAssetId = m.Id
            WHERE m.Id = '00000000-0000-0000-0000-000000010003';
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var contentType = reader.GetString(0);
        var contentLength = reader.GetInt64(1);
        var bytes = (byte[])reader["Content"];

        Assert.Equal("image/png", contentType);
        Assert.Equal(contentLength, bytes.LongLength);
        Assert.True(IsCompletePng(bytes));
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenEnabled_IncludesActiveSlotAndRecommendationSignalDemoEvents()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "EventSlotReservations", "Status = 0") >= 4);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "DiscountActivations", "IsActive = 1") >= 2);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "RestaurantSlots", "Status = 0 AND MinThresholdForDiscount IS NOT NULL AND DiscountPercent IS NOT NULL") >= 5);
        Assert.True(await CountRowsAsync(
            customFactory.ConnectionString,
            "Events e JOIN EventSlotReservations r ON e.Id = r.EventId JOIN DiscountActivations d ON r.Id = d.ReservationId JOIN EventParticipants p ON e.Id = p.EventId JOIN RestaurantCuisines rc ON e.SelectedRestaurantId = rc.RestaurantId",
            "e.Title = 'Sushi Budz Discount Table' AND r.Status = 0 AND d.IsActive = 1 AND d.IsFinalized = 0 AND p.UserId = '00000000-0000-0000-0000-000000000102' AND p.State = 1 AND rc.CuisineId IN ('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002')") >= 2);
        Assert.True(await CountRowsAsync(
            customFactory.ConnectionString,
            "Events e JOIN RestaurantCuisines rc ON e.SelectedRestaurantId = rc.RestaurantId",
            "e.Title = 'Sushi Preference Test Table' AND rc.CuisineId IN ('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002')") >= 2);
        Assert.True(await CountRowsAsync(
            customFactory.ConnectionString,
            "Events e JOIN EventParticipants p ON e.Id = p.EventId",
            "e.Title = 'Brooke Riverfront Grill Meetup' AND e.SelectedRestaurantId = '55555555-5555-5555-5555-555555555555' AND p.UserId = '00000000-0000-0000-0000-000000000102' AND p.State = 1") >= 1);
        Assert.True(await CountRowsAsync(
            customFactory.ConnectionString,
            "Events e JOIN EventParticipants p ON e.Id = p.EventId",
            "e.Title = 'Nearby Campus Noodles Walkup' AND e.SelectedRestaurantId = '44444444-4444-4444-4444-444444444444' AND p.UserId = '00000000-0000-0000-0000-000000000104' AND p.State = 1") >= 1);
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenDisabled_DoesNotPopulateDevelopmentAccounts()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "false",
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var connection = new SqliteConnection(customFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM UserAccounts;";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenDatabaseAlreadyHasUsers_DoesNotReapplySeedUsers()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });

        await SqliteDatabaseBootstrapper.RecreateDatabaseAsync(customFactory.ConnectionString);
        await InsertUserAsync(
            customFactory.ConnectionString,
            id: "99999999-9999-9999-9999-999999999999",
            username: "alex",
            email: "alex-existing@tastebudz.local");

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var connection = new SqliteConnection(customFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM UserAccounts;";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SeedTestDataOnStartup_WhenDatabaseAlreadyHasSeedUsers_AppliesIncrementalSeedRecords()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "true",
        });

        await SqliteDatabaseBootstrapper.RecreateDatabaseAsync(customFactory.ConnectionString);
        await InsertUserAsync(
            customFactory.ConnectionString,
            id: "00000000-0000-0000-0000-000000000101",
            username: "alex",
            email: "alex@tastebudz.local");

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/definitely-missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "UserAccounts") >= 10);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "Events") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "RestaurantSlots", "DiscountPercent IS NOT NULL") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ChatMessages") >= 1);
        Assert.True(await CountRowsAsync(customFactory.ConnectionString, "ModerationReports", "Status = 0") >= 1);
    }

    [Fact]
    public async Task SqliteBootstrapper_CreatesEventFeedbackSchema()
    {
        using var customFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Persistence:SeedTestDataOnStartup"] = "false",
        });

        await SqliteDatabaseBootstrapper.RecreateDatabaseAsync(customFactory.ConnectionString);

        Assert.True(await TableExistsAsync(customFactory.ConnectionString, "EventFeedbacks"));
        Assert.True(await TableExistsAsync(customFactory.ConnectionString, "EventFeedbackPhotos"));
        Assert.Contains("EventId", await ListColumnsAsync(customFactory.ConnectionString, "MediaAssets"));
        Assert.Contains("Rating", await ListColumnsAsync(customFactory.ConnectionString, "EventFeedbacks"));
        Assert.Contains("MediaAssetId", await ListColumnsAsync(customFactory.ConnectionString, "EventFeedbackPhotos"));
    }

    [Fact]
    public void CanonicalSqlScripts_IncludeEventFeedbackSchemaForBothProviders()
    {
        var root = FindRepositoryRoot();
        var sqliteScript = File.ReadAllText(Path.Combine(root, "src", "TasteBudz.Database", "sqlite", "dbTasteBudz.sqlite.sql"));
        var sqlServerScript = File.ReadAllText(Path.Combine(root, "src", "TasteBudz.Database", "sqlserver", "010_schema.sql"));

        Assert.Contains("CREATE TABLE IF NOT EXISTS EventFeedbacks", sqliteScript);
        Assert.Contains("CREATE TABLE IF NOT EXISTS EventFeedbackPhotos", sqliteScript);
        Assert.Contains("IX_MediaAssets_EventId", sqliteScript);
        Assert.Contains("CREATE TABLE IF NOT EXISTS PasswordResetRequests", sqliteScript);
        Assert.Contains("StreetAddress TEXT NULL", sqliteScript);
        Assert.Contains("IsArchived INTEGER NOT NULL DEFAULT 0", sqliteScript);
        Assert.Contains("CREATE TABLE dbo.EventFeedbacks", sqlServerScript);
        Assert.Contains("CREATE TABLE dbo.EventFeedbackPhotos", sqlServerScript);
        Assert.Contains("IX_MediaAssets_EventId", sqlServerScript);
        Assert.Contains("CREATE TABLE dbo.PasswordResetRequests", sqlServerScript);
        Assert.Contains("StreetAddress NVARCHAR(160) NULL", sqlServerScript);
        Assert.Contains("IsArchived BIT NOT NULL", sqlServerScript);
    }

    [Fact]
    public void AzureSqlDemoDataScripts_EnableRequiredSqlServerSetOptions()
    {
        var root = FindRepositoryRoot();
        var scriptPaths = new[]
        {
            Path.Combine(root, "src", "TasteBudz.Database", "sqlserver", "demo", "20260426_feature_seed_topup.sql"),
            Path.Combine(root, "src", "TasteBudz.Database", "sqlserver", "demo", "20260426_feature_seed_topup_rollback.sql"),
        };

        foreach (var scriptPath in scriptPaths)
        {
            var script = File.ReadAllText(scriptPath);

            Assert.Contains("SET ANSI_NULLS ON;", script);
            Assert.Contains("SET QUOTED_IDENTIFIER ON;", script);
        }
    }

    private static async Task DropTableAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE {tableName};";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateOldMediaAssetsTableAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE MediaAssets (
                Id TEXT NOT NULL PRIMARY KEY,
                OwnerUserId TEXT NOT NULL,
                ProfileUserId TEXT NULL,
                GroupId TEXT NULL,
                EventId TEXT NULL,
                StorageUrl TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateOldRestaurantSlotsTableWithoutDiscountPercentAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE RestaurantSlots (
                Id TEXT NOT NULL PRIMARY KEY,
                RestaurantId TEXT NOT NULL,
                StartsAtUtc TEXT NOT NULL,
                EndsAtUtc TEXT NOT NULL,
                Capacity INTEGER NOT NULL,
                CutoffAtUtc TEXT NOT NULL,
                MinThresholdForDiscount INTEGER NULL,
                Status INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                CancelledAtUtc TEXT NULL,
                CancellationReason TEXT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
        command.Parameters.AddWithValue("$tableName", tableName);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<IReadOnlyCollection<string>> ListColumnsAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static async Task<int> CountRowsAsync(string connectionString, string tableName, string whereClause = "1 = 1")
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause};";

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static bool IsCompletePng(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        if (bytes.Length < signature.Length + 12 || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
        {
            return false;
        }

        var offset = signature.Length;
        var sawHeader = false;

        while (offset <= bytes.Length - 12)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            offset += 4;

            if (length < 0 || offset + 4 + length + 4 > bytes.Length)
            {
                return false;
            }

            var chunkType = bytes.AsSpan(offset, 4);
            offset += 4;

            if (chunkType.SequenceEqual("IHDR"u8))
            {
                sawHeader = true;
            }

            offset += length + 4;

            if (chunkType.SequenceEqual("IEND"u8))
            {
                return sawHeader && offset == bytes.Length;
            }
        }

        return false;
    }

    private static async Task InsertUserAsync(string connectionString, string id, string username, string email)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO UserAccounts
                (Id, Username, NormalizedUsername, Email, NormalizedEmail, PasswordHash, Status, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
            VALUES
                ($id, $username, $normalizedUsername, $email, $normalizedEmail, $passwordHash, 0, '2026-03-30T12:00:00Z', '2026-03-30T12:00:00Z', NULL);
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$normalizedUsername", username.ToUpperInvariant());
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$normalizedEmail", email.ToUpperInvariant());
        command.Parameters.AddWithValue("$passwordHash", "seed-test-hash");
        await command.ExecuteNonQueryAsync();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TasteBudz.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
