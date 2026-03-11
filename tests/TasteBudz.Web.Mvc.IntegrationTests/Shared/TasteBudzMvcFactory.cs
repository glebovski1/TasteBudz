using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public sealed class TasteBudzMvcFactory : WebApplicationFactory<Program>
{
    public StubBackendApiHandler BackendHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackendApi:BaseUrl"] = "https://backend.test",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(BackendHandler);
            services.AddHttpClient("BackendApi")
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<StubBackendApiHandler>());
            services.AddHttpClient("BackendApiAuth")
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<StubBackendApiHandler>());
        });
    }
}
