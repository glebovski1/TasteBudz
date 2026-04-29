using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;


internal sealed class InMemorySession : ISession
{
    private readonly Dictionary<string, byte[]> values = [];

    public IEnumerable<string> Keys => values.Keys;

    public string Id { get; } = Guid.NewGuid().ToString("N");

    public bool IsAvailable => true;

    public void Clear() => values.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => values.Remove(key);

    public void Set(string key, byte[] value) => values[key] = value;

    public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
}
