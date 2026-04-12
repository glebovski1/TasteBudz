using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Web.Mvc.Controllers;

namespace TasteBudz.Web.Mvc.IntegrationTests.Configuration;

public sealed class BackendApiConfigurationTests
{
    [Fact]
    public void DevelopmentSettings_UseSingleHostBackendFallbackAndSqlite()
    {
        var mvcProjectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "TasteBudz.Web.Mvc"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(mvcProjectDirectory)
            .AddJsonFile("appsettings.Development.json")
            .Build();

        Assert.Equal(string.Empty, configuration["BackendApi:BaseUrl"]);
        Assert.Equal("Sqlite", configuration["Persistence:Provider"]);
        Assert.True(configuration.GetValue<bool>("Persistence:InitializeSqliteOnStartup"));
    }

    [Fact]
    public void BackendApiNamedClient_DisablesAutomaticRedirects()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "TasteBudz.MvcConfigurationTests", $"{Guid.NewGuid():N}.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        using var factory = new WebApplicationFactory<AccountController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TasteBudz"] = $"Data Source={databasePath};Foreign Keys=True;Pooling=False",
                        ["Persistence:Provider"] = "Sqlite",
                        ["Persistence:InitializeSqliteOnStartup"] = "true",
                        ["Persistence:SeedTestDataOnStartup"] = "false",
                    });
                });
            });

        try
        {
            var handlerFactory = factory.Services.GetRequiredService<IHttpMessageHandlerFactory>();
            using var handler = handlerFactory.CreateHandler("BackendApi");
            var primaryHandler = FindPrimaryHandler(handler);

            Assert.NotNull(primaryHandler);
            Assert.False(primaryHandler.AllowAutoRedirect);
        }
        finally
        {
            TryDeleteFile(databasePath);
            TryDeleteFile($"{databasePath}-shm");
            TryDeleteFile($"{databasePath}-wal");
        }
    }

    private static HttpClientHandler? FindPrimaryHandler(HttpMessageHandler handler)
    {
        HttpMessageHandler? current = handler;

        while (current is not null)
        {
            if (current is HttpClientHandler httpClientHandler)
            {
                return httpClientHandler;
            }

            var innerHandlerProperty = current.GetType().GetProperty(
                "InnerHandler",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            current = innerHandlerProperty?.GetValue(current) as HttpMessageHandler;
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
