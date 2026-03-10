// Integration tests for availability HTTP edge cases.
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that update endpoints fail cleanly when the requested availability window does not exist.
/// </summary>
public sealed class AvailabilityApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    /// <summary>
    /// Missing recurring and one-off ids should both surface as ProblemDetails 404 responses.
    /// </summary>
    [Fact]
    public async Task PatchMissingAvailabilityWindows_ReturnNotFoundProblemDetails()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        // One authenticated user is enough because the missing-resource behavior is user-scoped, not role-scoped.
        var session = await ApiTestHelpers.RegisterAsync(client, username: "sam", email: "sam@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var recurringResponse = await client.PatchAsJsonAsync($"/api/v1/availability/recurring/{Guid.NewGuid()}", new UpsertRecurringAvailabilityWindowRequest
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(21, 0),
            Label = "Friday dinner",
        });

        var oneOffResponse = await client.PatchAsJsonAsync($"/api/v1/availability/one-off/{Guid.NewGuid()}", new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 14, 18, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 14, 21, 0, 0, TimeSpan.Zero),
            Label = "Saturday dinner",
        });

        var recurringProblem = await recurringResponse.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);
        var oneOffProblem = await oneOffResponse.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, recurringResponse.StatusCode);
        Assert.Contains("application/problem+json", recurringResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, recurringProblem!.Status);
        Assert.Equal(HttpStatusCode.NotFound, oneOffResponse.StatusCode);
        Assert.Contains("application/problem+json", oneOffResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, oneOffProblem!.Status);
    }
}
