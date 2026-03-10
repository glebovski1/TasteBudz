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
    [Fact]
    public async Task OneOffAvailabilityEndpoints_SupportCreateUpdateListAndDelete()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "sam", email: "sam@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/v1/availability/one-off", new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 14, 18, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 14, 21, 0, 0, TimeSpan.Zero),
            Label = "  Saturday dinner  ",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<OneOffAvailabilityWindowDto>(ApiTestHelpers.JsonOptions);
        var createdId = created!.Id;

        var listResponse = await client.GetAsync("/api/v1/availability/one-off");
        var listed = await listResponse.Content.ReadFromJsonAsync<OneOffAvailabilityWindowDto[]>(ApiTestHelpers.JsonOptions);
        var listedItem = Assert.Single(listed!);

        var updateResponse = await client.PatchAsJsonAsync($"/api/v1/availability/one-off/{createdId}", new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 14, 19, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 14, 22, 0, 0, TimeSpan.Zero),
            Label = "  Updated dinner  ",
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<OneOffAvailabilityWindowDto>(ApiTestHelpers.JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/v1/availability/one-off/{createdId}");
        var listAfterDeleteResponse = await client.GetAsync("/api/v1/availability/one-off");
        var afterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<OneOffAvailabilityWindowDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal("Saturday dinner", created.Label);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(createdId, listedItem.Id);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(createdId, updated!.Id);
        Assert.Equal("Updated dinner", updated.Label);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty(afterDelete!);
    }

    [Fact]
    public async Task CreateOneOffAvailability_WithInvalidRange_ReturnsBadRequestProblemDetails()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "sam", email: "sam@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/availability/one-off", new UpsertOneOffAvailabilityWindowRequest
        {
            StartsAtUtc = new DateTimeOffset(2026, 3, 14, 21, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2026, 3, 14, 18, 0, 0, TimeSpan.Zero),
            Label = "Backwards",
        });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("One-off availability startsAtUtc must be earlier than endsAtUtc.", problem.Detail);
    }

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
