using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Web.Mvc.Services.Backend;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public sealed class StubBackendApiHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<ExpectedBackendRequest> expectations = new();

    public List<RecordedBackendRequest> Requests { get; } = [];

    public void Enqueue(
        HttpMethod method,
        string pathAndQuery,
        Func<HttpRequestMessage, string?, HttpResponseMessage> responder)
    {
        expectations.Enqueue(new ExpectedBackendRequest(method, pathAndQuery, responder));
    }

    public void AssertDrained() => Assert.Empty(expectations);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!expectations.TryDequeue(out var expected))
        {
            throw new InvalidOperationException($"Unexpected backend call: {request.Method} {request.RequestUri?.PathAndQuery}");
        }

        Assert.Equal(expected.Method, request.Method);
        Assert.Equal(expected.PathAndQuery, request.RequestUri?.PathAndQuery);

        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedBackendRequest(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            body,
            request.Headers.Authorization?.Scheme,
            request.Headers.Authorization?.Parameter));

        return expected.Responder(request, body);
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, object payload) =>
        new(statusCode)
        {
            Content = JsonContent.Create(payload, options: BackendJson.Options),
        };

    public static HttpResponseMessage Problem(HttpStatusCode statusCode, string title, string detail) =>
        new(statusCode)
        {
            Content = JsonContent.Create(
                new
                {
                    status = (int)statusCode,
                    title,
                    detail,
                    instance = "/",
                },
                options: BackendJson.Options),
        };
}

public sealed record ExpectedBackendRequest(
    HttpMethod Method,
    string PathAndQuery,
    Func<HttpRequestMessage, string?, HttpResponseMessage> Responder);

public sealed record RecordedBackendRequest(
    HttpMethod Method,
    string PathAndQuery,
    string? Body,
    string? AuthorizationScheme,
    string? AuthorizationParameter);
