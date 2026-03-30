// Shared WebApplicationFactory configuration for backend integration tests.
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.IntegrationTests.Shared;

/// <summary>
/// Boots the real API host in an integration-testing environment and manages an isolated temporary SQLite database per factory instance.
/// </summary>
public sealed class TasteBudzApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString;
    private readonly string databasePath;
    private readonly IReadOnlyDictionary<string, string?> userConfigurationOverrides;
    private int cleanupPerformed;

    public string ConnectionString => connectionString;

    public string DatabasePath => databasePath;

    public TasteBudzApiFactory()
        : this(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase), CreateDatabasePath())
    {
    }

    private TasteBudzApiFactory(IReadOnlyDictionary<string, string?> configurationOverrides, string databasePath)
    {
        userConfigurationOverrides = new Dictionary<string, string?>(configurationOverrides, StringComparer.OrdinalIgnoreCase);
        this.databasePath = databasePath;
        connectionString = $"Data Source={databasePath};Foreign Keys=True;Pooling=False";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        builder.ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(BuildConfigurationOverrides()));
    }

    public void ResetState()
    {
        SqliteDatabaseBootstrapper.RecreateDatabaseAsync(connectionString).GetAwaiter().GetResult();
    }

    public TasteBudzApiFactory WithConfigurationOverrides(IReadOnlyDictionary<string, string?> overrides)
    {
        var merged = new Dictionary<string, string?>(userConfigurationOverrides, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in overrides)
        {
            merged[pair.Key] = pair.Value;
        }

        return new TasteBudzApiFactory(merged, CreateDatabasePath());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        CleanupDatabaseFiles();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CleanupDatabaseFiles();
    }

    private Dictionary<string, string?> BuildConfigurationOverrides()
    {
        var overrides = new Dictionary<string, string?>(userConfigurationOverrides, StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:TasteBudz"] = connectionString,
        };

        overrides.TryAdd("Persistence:InitializeSqliteOnStartup", "true");

        return overrides;
    }

    private static string CreateDatabasePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TasteBudz.IntegrationTests");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{Guid.NewGuid():N}.sqlite");
    }

    private void CleanupDatabaseFiles()
    {
        if (Interlocked.Exchange(ref cleanupPerformed, 1) != 0)
        {
            return;
        }

        TryDeleteFile(databasePath);
        TryDeleteFile($"{databasePath}-shm");
        TryDeleteFile($"{databasePath}-wal");

        var directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }

    private static void TryDeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
