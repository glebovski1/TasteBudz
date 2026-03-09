// Shared helpers for HTTP-level integration tests.
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.IntegrationTests.Shared;

/// <summary>
/// Keeps common test host JSON settings and auth helper routines in one place.
/// </summary>
internal static class ApiTestHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    internal static async Task<SessionDto> RegisterAsync(HttpClient client, string username = "alex", string email = "alex@example.com", string zipCode = "45220")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = zipCode,
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionDto>(JsonOptions))!;
    }

    internal static void SetBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    internal static async Task PromoteRolesAsync(IServiceProvider serviceProvider, Guid userId, IReadOnlyCollection<UserRole> roles)
    {
        using var scope = serviceProvider.CreateScope();
        var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
        var account = await authRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Cannot promote a missing test user.");

        await authRepository.UpdateAccountAsync(account with
        {
            Roles = roles,
        });
    }
}
