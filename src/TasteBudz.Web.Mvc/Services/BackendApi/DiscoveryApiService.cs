using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over discovery, swipe, and Budz endpoints.
/// </summary>
public sealed class DiscoveryApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public DiscoveryApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<ListResponse<DiscoveryProfilePreviewDto>> SearchPeopleAsync(
        SearchPeopleQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<DiscoveryProfilePreviewDto>>(
            BuildSearchPeoplePath(query ?? new SearchPeopleQuery()),
            cancellationToken);

    public Task<ListResponse<DiscoveryProfilePreviewDto>> GetSwipeCandidatesAsync(
        SwipeCandidatesQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<DiscoveryProfilePreviewDto>>(
            BuildSwipeCandidatesPath(query ?? new SwipeCandidatesQuery()),
            cancellationToken);

    public Task<SwipeDecisionResultDto> RecordSwipeAsync(
        RecordSwipeDecisionRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<RecordSwipeDecisionRequest, SwipeDecisionResultDto>(
            "/api/v1/discovery/swipes",
            request,
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<BudConnectionDto>> ListBudzAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<BudConnectionDto>>("/api/v1/budz", cancellationToken);

    private static string BuildSearchPeoplePath(SearchPeopleQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            builder.Add("q", query.Q);
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/discovery/people{builder.ToQueryString()}";
    }

    private static string BuildSwipeCandidatesPath(SwipeCandidatesQuery query)
    {
        var builder = new QueryBuilder
        {
            { "page", query.Page.ToString(CultureInfo.InvariantCulture) },
            { "pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture) },
        };

        return $"/api/v1/discovery/swipe-candidates{builder.ToQueryString()}";
    }
}
