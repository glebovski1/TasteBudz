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
    public void DevelopmentSettings_UseBackendHttpsBaseUrl()
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

        Assert.Equal("https://localhost:7118", configuration["BackendApi:BaseUrl"]);
    }

    [Fact]
    public void BackendApiNamedClient_DisablesAutomaticRedirects()
    {
        using var factory = new WebApplicationFactory<AccountController>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

        var handlerFactory = factory.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        using var handler = handlerFactory.CreateHandler("BackendApi");
        var primaryHandler = FindPrimaryHandler(handler);

        Assert.NotNull(primaryHandler);
        Assert.False(primaryHandler.AllowAutoRedirect);
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
}
