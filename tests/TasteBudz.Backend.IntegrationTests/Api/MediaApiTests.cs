using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.IntegrationTests.Api;

public sealed class MediaApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task ProfileAvatarEndpoints_StoreAndServeImageBytes()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var viewerSession = await ApiTestHelpers.RegisterAsync(viewerClient, username: "viewer", email: "viewer@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(viewerClient, viewerSession.AccessToken);

        var avatarBytes = new byte[] { 1, 3, 5, 7, 9 };
        using var content = CreateImageUpload("avatar.png", "image/png", avatarBytes);
        var uploadResponse = await ownerClient.PostAsync("/api/v1/profiles/me/avatar", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>(ApiTestHelpers.JsonOptions);

        var ownerProfile = await (await ownerClient.GetAsync("/api/v1/profiles/me")).Content.ReadFromJsonAsync<ProfileDto>(ApiTestHelpers.JsonOptions);
        var publicProfile = await (await viewerClient.GetAsync($"/api/v1/profiles/{ownerSession.CurrentUser.UserId}")).Content.ReadFromJsonAsync<ProfileDto>(ApiTestHelpers.JsonOptions);
        var mediaResponse = await viewerClient.GetAsync($"/api/v1/media/{uploaded!.MediaAssetId}");
        var storedBytes = await mediaResponse.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        Assert.Equal(uploaded.MediaAssetId, ownerProfile!.AvatarMediaAssetId);
        Assert.Equal(uploaded.MediaAssetId, publicProfile!.AvatarMediaAssetId);
        Assert.Equal(HttpStatusCode.OK, mediaResponse.StatusCode);
        Assert.Equal("image/png", mediaResponse.Content.Headers.ContentType!.MediaType);
        Assert.Equal(avatarBytes, storedBytes);
    }

    [Fact]
    public async Task ReportAttachmentEndpoints_RestrictAccessToReporterAndModerators()
    {
        factory.ResetState();
        using var reporterClient = factory.CreateClient();
        using var moderatorClient = factory.CreateClient();
        using var outsiderClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "moderator", email: "moderator@example.com");
        var outsiderSession = await ApiTestHelpers.RegisterAsync(outsiderClient, username: "outsider", email: "outsider@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");

        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(outsiderClient, outsiderSession.AccessToken);

        var reportResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Harassment",
            Reason = "Repeated abuse",
        });
        var report = await reportResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var attachmentBytes = new byte[] { 8, 6, 4, 2 };
        using var uploadContent = CreateImageUpload("evidence.png", "image/png", attachmentBytes);
        var uploadResponse = await reporterClient.PostAsync($"/api/v1/reports/{report!.ReportId}/attachments", uploadContent);
        var attachment = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>(ApiTestHelpers.JsonOptions);

        var reporterListResponse = await reporterClient.GetAsync($"/api/v1/reports/{report.ReportId}/attachments");
        var reporterAttachments = await reporterListResponse.Content.ReadFromJsonAsync<MediaAssetDto[]>(ApiTestHelpers.JsonOptions);
        var moderatorListResponse = await moderatorClient.GetAsync($"/api/v1/reports/{report.ReportId}/attachments");
        var moderatorAttachments = await moderatorListResponse.Content.ReadFromJsonAsync<MediaAssetDto[]>(ApiTestHelpers.JsonOptions);
        var outsiderListResponse = await outsiderClient.GetAsync($"/api/v1/reports/{report.ReportId}/attachments");

        var moderatorMediaResponse = await moderatorClient.GetAsync($"/api/v1/media/{attachment!.MediaAssetId}");
        var moderatorBytes = await moderatorMediaResponse.Content.ReadAsByteArrayAsync();
        var outsiderMediaResponse = await outsiderClient.GetAsync($"/api/v1/media/{attachment.MediaAssetId}");

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        Assert.Contains(reporterAttachments!, item => item.MediaAssetId == attachment.MediaAssetId);
        Assert.Contains(moderatorAttachments!, item => item.MediaAssetId == attachment.MediaAssetId);
        Assert.Equal(HttpStatusCode.Forbidden, outsiderListResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, moderatorMediaResponse.StatusCode);
        Assert.Equal(attachmentBytes, moderatorBytes);
        Assert.Equal(HttpStatusCode.Forbidden, outsiderMediaResponse.StatusCode);
    }

    private static MultipartFormDataContent CreateImageUpload(string fileName, string contentType, byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "File", fileName);
        return content;
    }
}
