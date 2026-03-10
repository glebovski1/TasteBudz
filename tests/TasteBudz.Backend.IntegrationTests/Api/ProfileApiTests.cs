// Integration tests for current-user profile, preference, privacy, and availability endpoints.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that profile-side endpoints persist changes through the full HTTP pipeline.
/// </summary>
public sealed class ProfileApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task ProfilePreferencesAndPrivacyEndpoints_PersistCurrentUserChanges()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "sam", email: "sam@example.com", zipCode: "45219");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var profileResponse = await client.PatchAsJsonAsync("/api/v1/profiles/me", new UpdateMyProfileRequest
        {
            DisplayName = "Sam Carter",
            Bio = "Always down for ramen.",
            HomeAreaZipCode = "45220",
            SocialGoal = SocialGoal.Friends,
        });
        profileResponse.EnsureSuccessStatusCode();

        var preferenceResponse = await client.PutAsJsonAsync("/api/v1/preferences/me", new ReplacePreferencesRequest
        {
            CuisineTags = new[] { "Ramen", "Sushi" },
            SpiceTolerance = SpiceTolerance.Medium,
            DietaryFlags = new[] { "Vegetarian" },
            Allergies = new[] { "Peanuts" },
        });
        preferenceResponse.EnsureSuccessStatusCode();

        var privacyResponse = await client.PatchAsJsonAsync("/api/v1/privacy-settings/me", new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });
        privacyResponse.EnsureSuccessStatusCode();

        var availabilityResponse = await client.PostAsJsonAsync("/api/v1/availability/recurring", new UpsertRecurringAvailabilityWindowRequest
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(21, 0),
            Label = "Friday dinner",
        });
        availabilityResponse.EnsureSuccessStatusCode();

        var profile = await (await client.GetAsync("/api/v1/profiles/me")).Content.ReadFromJsonAsync<ProfileDto>(ApiTestHelpers.JsonOptions);
        var preferences = await (await client.GetAsync("/api/v1/preferences/me")).Content.ReadFromJsonAsync<PreferenceDto>(ApiTestHelpers.JsonOptions);
        var privacy = await (await client.GetAsync("/api/v1/privacy-settings/me")).Content.ReadFromJsonAsync<PrivacySettingsDto>(ApiTestHelpers.JsonOptions);
        var recurring = await (await client.GetAsync("/api/v1/availability/recurring")).Content.ReadFromJsonAsync<RecurringAvailabilityWindowDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal("Sam Carter", profile!.DisplayName);
        Assert.Equal("Always down for ramen.", profile.Bio);
        Assert.Equal("45220", profile.HomeAreaZipCode);
        Assert.Equal(SocialGoal.Friends, profile.SocialGoal);
        Assert.Equal(2, preferences!.CuisineTags.Count);
        Assert.False(privacy!.DiscoveryEnabled);
        Assert.Single(recurring!);
    }

    [Fact]
    public async Task BlocksEndpoint_PersistsListAndRemoval()
    {
        factory.ResetState();
        using var alexClient = factory.CreateClient();
        using var samClient = factory.CreateClient();

        var alexSession = await ApiTestHelpers.RegisterAsync(alexClient, username: "alex", email: "alex@example.com");
        var samSession = await ApiTestHelpers.RegisterAsync(samClient, username: "sam", email: "sam@example.com");
        ApiTestHelpers.SetBearer(alexClient, alexSession.AccessToken);

        var createResponse = await alexClient.PostAsJsonAsync("/api/v1/blocks", new CreateBlockRequest
        {
            BlockedUserId = samSession.CurrentUser.UserId,
        });
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await alexClient.GetAsync("/api/v1/blocks");
        var blocks = await listResponse.Content.ReadFromJsonAsync<BlockedUserDto[]>(ApiTestHelpers.JsonOptions);

        var deleteResponse = await alexClient.DeleteAsync($"/api/v1/blocks/{samSession.CurrentUser.UserId}");
        var listAfterDeleteResponse = await alexClient.GetAsync("/api/v1/blocks");
        var blocksAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<BlockedUserDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(blocks!, block => block.UserId == samSession.CurrentUser.UserId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty(blocksAfterDelete!);
    }

    [Fact]
    public async Task PrivacyPatch_WithOmittedField_DoesNotResetExistingValue()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "privacy", email: "privacy@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var disableResponse = await client.PatchAsJsonAsync("/api/v1/privacy-settings/me", new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });
        disableResponse.EnsureSuccessStatusCode();

        var noOpPatchResponse = await client.PatchAsJsonAsync("/api/v1/privacy-settings/me", new { });
        noOpPatchResponse.EnsureSuccessStatusCode();

        var privacy = await (await client.GetAsync("/api/v1/privacy-settings/me")).Content.ReadFromJsonAsync<PrivacySettingsDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, noOpPatchResponse.StatusCode);
        Assert.False(privacy!.DiscoveryEnabled);
    }
}
