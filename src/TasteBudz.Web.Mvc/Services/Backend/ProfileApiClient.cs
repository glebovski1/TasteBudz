using TasteBudz.Web.Mvc.Services.Backend.Contracts;

namespace TasteBudz.Web.Mvc.Services.Backend;

public sealed class ProfileApiClient(BackendApiRequestExecutor executor)
{
    public Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        executor.GetAsync<ProfileDto>("/api/v1/profiles/me", cancellationToken);

    public Task<ProfileDto> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default) =>
        executor.PatchAsync<UpdateMyProfileRequest, ProfileDto>("/api/v1/profiles/me", request, cancellationToken);
}
