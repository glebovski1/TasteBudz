using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;


internal sealed class SingleClientFactory : IHttpClientFactory
{
    private readonly HttpClient httpClient;

    public SingleClientFactory(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public HttpClient CreateClient(string name) => httpClient;
}
