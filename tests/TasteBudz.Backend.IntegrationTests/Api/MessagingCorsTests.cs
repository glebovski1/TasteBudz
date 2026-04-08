using System.Net;
using TasteBudz.Backend.IntegrationTests.Shared;

namespace TasteBudz.Backend.IntegrationTests.Api;

public sealed class MessagingCorsTests
{
    [Fact]
    public async Task ChatHubNegotiate_WhenOriginIsAllowed_ReturnsCorsHeaders()
    {
        using var factory = new TasteBudzApiFactory().WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:7115",
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/hubs/chat/negotiate?negotiateVersion=1");
        request.Headers.Add("Origin", "https://localhost:7115");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type,x-requested-with");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://localhost:7115", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }
}
