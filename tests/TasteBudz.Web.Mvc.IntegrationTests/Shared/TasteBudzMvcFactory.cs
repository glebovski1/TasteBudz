using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Web.Mvc.Controllers;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public sealed class TasteBudzMvcFactory : WebApplicationFactory<AccountController>
{
    private readonly string databasePath = CreateDatabasePath();
    private int cleanupPerformed;

    public StubBackendApiHandler BackendHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackendApi:BaseUrl"] = "https://backend.test",
                ["ConnectionStrings:TasteBudz"] = $"Data Source={databasePath};Foreign Keys=True;Pooling=False",
                ["Persistence:Provider"] = "Sqlite",
                ["Persistence:InitializeSqliteOnStartup"] = "true",
                ["Persistence:SeedTestDataOnStartup"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(BackendHandler);
            services.AddHttpClient("BackendApi")
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<StubBackendApiHandler>());
        });
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

    private static string CreateDatabasePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TasteBudz.MvcIntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "tastebudz.sqlite");
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
            Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory);
            }
            catch (DirectoryNotFoundException)
            {
                // Parallel test cleanup can remove the shared temp folder first.
            }
            catch (IOException)
            {
                // Other test factories may still be creating or deleting database files.
            }
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
