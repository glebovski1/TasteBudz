using System.Net;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class HomeMvcTests
{
    [Fact]
    public async Task HomePage_AdvertisesDefaultEnabledChatScopes()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("event and group chats", html);
        Assert.Contains("support", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("one-on-one", html, StringComparison.OrdinalIgnoreCase);
    }
}
