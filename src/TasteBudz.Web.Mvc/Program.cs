using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using TasteBudz.Web.Mvc.Options;
using TasteBudz.Web.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
var mvcSessionIdleTimeout = TimeSpan.FromHours(8);

builder.Services
    .AddOptions<BackendApiOptions>()
    .Bind(builder.Configuration.GetSection(BackendApiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "BackendApi:BaseUrl must be an absolute URI.")
    .ValidateOnStart();

builder.Services.AddControllersWithViews();
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

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

// Register the MVC app's backend-facing service layer inline.
// Controllers ask for these concrete services in their constructors, and ASP.NET Core DI supplies them per request.
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<BackendSessionCookieEvents>();
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
// BackendHttpClient asks IHttpClientFactory for this named client whenever it needs to call the API.
builder.Services.AddHttpClient("BackendApi", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Protected backend calls must fail fast on redirects so auth headers are not silently lost.
        AllowAutoRedirect = false,
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
app.UseAntiforgery();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
