using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Controllers;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Configuration;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Web.Mvc.Options;
using TasteBudz.Web.Mvc.Services;

const string HostAuthenticationScheme = "TasteBudzHost";

var builder = WebApplication.CreateBuilder(args);
var mvcSessionIdleTimeout = TimeSpan.FromHours(8);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors();

builder.Services
    .AddOptions<BackendApiOptions>()
    .Bind(builder.Configuration.GetSection(BackendApiOptions.SectionName))
    .Validate(
        options => string.IsNullOrWhiteSpace(options.BaseUrl) ||
            Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "BackendApi:BaseUrl must be blank or an absolute URI.")
    .ValidateOnStart();

builder.Services
    .AddControllersWithViews()
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TasteBudz.Mvc.Session";
    options.IdleTimeout = mvcSessionIdleTimeout;
});

// Register the modular backend services and shared infrastructure used by the single host.
builder.Services.AddTasteBudzFoundation(builder.Configuration);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = HostAuthenticationScheme;
        options.DefaultChallengeScheme = HostAuthenticationScheme;
        options.DefaultForbidScheme = HostAuthenticationScheme;
    })
    .AddPolicyScheme(HostAuthenticationScheme, HostAuthenticationScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
            IsBackendEndpoint(context.Request.Path)
                ? SessionAuthenticationDefaults.Scheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = ".TasteBudz.Mvc.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = mvcSessionIdleTimeout;
        options.SlidingExpiration = true;
        options.EventsType = typeof(BackendSessionCookieEvents);
    });

// Register the MVC app's backend-facing service layer inline.
// Controllers ask for these concrete services in their constructors, and ASP.NET Core DI supplies them per request.
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<BackendSessionCookieEvents>();
builder.Services.AddScoped<IBackendApiBaseAddressProvider, BackendApiBaseAddressProvider>();
builder.Services.AddScoped<BackendHttpClient>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProfileApiService>();
builder.Services.AddScoped<RestaurantApiService>();
builder.Services.AddScoped<EventApiService>();
builder.Services.AddScoped<GroupApiService>();
builder.Services.AddScoped<DiscoveryApiService>();
builder.Services.AddScoped<MessagingApiService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<ModerationApiService>();

// Register one named HttpClient for all backend calls.
// The base address can come from BackendApi:BaseUrl or fall back to the current single-host request URL.
builder.Services.AddHttpClient("BackendApi", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;

    if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var configuredBaseUrl))
    {
        client.BaseAddress = configuredBaseUrl;
    }
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Protected backend calls must fail fast on redirects so auth headers are not silently lost.
        AllowAutoRedirect = false,
    });

var app = builder.Build();
var allowedCorsOrigins = app.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

await app.EnsureTasteBudzPersistenceReadyAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
app.UseCors(policy =>
{
    if (allowedCorsOrigins.Length > 0)
    {
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
});
app.UseAntiforgery();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool IsBackendEndpoint(PathString path) =>
    path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
    path.StartsWithSegments("/hubs/chat", StringComparison.OrdinalIgnoreCase);

public partial class Program;
