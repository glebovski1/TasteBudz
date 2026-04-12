using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Payments;

namespace TasteBudz.Backend.IntegrationTests.Api;

public sealed class CheckoutApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task Checkout_WhenFeatureDisabled_ReturnsNotFoundProblemDetails()
    {
        factory.ResetState();
        using var client = factory.CreateClient();
        var session = await ApiTestHelpers.RegisterAsync(client, username: "host", email: "host@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);
        var eventDetail = await CreateEventAsync(client);

        var response = await client.PostAsync($"/api/v1/events/{eventDetail.EventId}/checkout-sessions", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, problem!.Status);
    }

    [Fact]
    public async Task Checkout_WhenFeatureEnabled_CreatesAndCompletesSession()
    {
        using var enabledFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["FeatureFlags:PaymentsCheckoutEnabled"] = "true",
        });
        enabledFactory.ResetState();
        using var client = enabledFactory.CreateClient();
        var session = await ApiTestHelpers.RegisterAsync(client, username: "host", email: "host@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);
        var eventDetail = await CreateEventAsync(client);

        var createResponse = await client.PostAsync($"/api/v1/events/{eventDetail.EventId}/checkout-sessions", null);
        var checkout = await createResponse.Content.ReadFromJsonAsync<CheckoutSessionDto>(ApiTestHelpers.JsonOptions);
        var completeResponse = await client.PostAsync($"/api/v1/checkout-sessions/{checkout!.CheckoutSessionId}/completion", null);
        var completed = await completeResponse.Content.ReadFromJsonAsync<CheckoutSessionDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(CheckoutSessionStatus.Pending, checkout.Status);
        Assert.Equal(2500, checkout.TotalCents);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.Equal(CheckoutSessionStatus.Completed, completed!.Status);
        Assert.NotNull(completed.CompletedAtUtc);
    }

    [Fact]
    public async Task Checkout_WhenFeatureEnabled_CreatesAndCancelsSession()
    {
        using var enabledFactory = factory.WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["FeatureFlags:PaymentsCheckoutEnabled"] = "true",
        });
        enabledFactory.ResetState();
        using var client = enabledFactory.CreateClient();
        var session = await ApiTestHelpers.RegisterAsync(client, username: "cancelhost", email: "cancelhost@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);
        var eventDetail = await CreateEventAsync(client);

        var createResponse = await client.PostAsync($"/api/v1/events/{eventDetail.EventId}/checkout-sessions", null);
        var checkout = await createResponse.Content.ReadFromJsonAsync<CheckoutSessionDto>(ApiTestHelpers.JsonOptions);
        var cancelResponse = await client.PostAsync($"/api/v1/checkout-sessions/{checkout!.CheckoutSessionId}/cancellation", null);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<CheckoutSessionDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        Assert.Equal(CheckoutSessionStatus.Cancelled, cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAtUtc);
    }

    private static async Task<EventDetailDto> CreateEventAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/events",
            new CreateEventRequest
            {
                Title = "Checkout event",
                EventType = EventType.Open,
                EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                Capacity = 4,
                SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            },
            ApiTestHelpers.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions))!;
    }
}
