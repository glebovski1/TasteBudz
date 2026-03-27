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

    private static async Task DropTableAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE {tableName};";
        await command.ExecuteNonQueryAsync();
    }
}
