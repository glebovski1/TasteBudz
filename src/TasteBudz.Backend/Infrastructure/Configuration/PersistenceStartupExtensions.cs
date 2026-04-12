using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Infrastructure.Persistence;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.Persistence.SqlServer;

namespace TasteBudz.Backend.Infrastructure.Configuration;

public static class PersistenceStartupExtensions
{
    public static async Task EnsureTasteBudzPersistenceReadyAsync(this WebApplication app)
    {
        var persistenceOptions = app.Services.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        var provider = PersistenceProviderNames.Normalize(persistenceOptions.Provider);
        var connectionString = app.Configuration.GetConnectionString("TasteBudz")
            ?? throw new InvalidOperationException("ConnectionStrings:TasteBudz must be configured.");

        if (string.Equals(provider, PersistenceProviderNames.SqlServer, StringComparison.Ordinal))
        {
            await SqlServerDatabaseReadinessValidator.ValidateRequiredSchemaAsync(
                connectionString,
                app.Logger,
                app.Lifetime.ApplicationStopping);
            return;
        }

        var normalizedConnectionString = SqliteConnectionStringHelper.Normalize(
            connectionString,
            app.Environment.ContentRootPath);

        await SqliteDatabaseBootstrapper.EnsureInitializedAsync(
            normalizedConnectionString,
            persistenceOptions.InitializeSqliteOnStartup,
            persistenceOptions.SeedTestDataOnStartup,
            app.Environment.EnvironmentName,
            app.Logger,
            app.Lifetime.ApplicationStopping);
    }
}
