// Basic integration smoke tests for host startup and routing.
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

    private static async Task DropTableAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE {tableName};";
        await command.ExecuteNonQueryAsync();
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
}
