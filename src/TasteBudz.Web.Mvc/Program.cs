using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using TasteBudz.Web.Mvc.Options;
using TasteBudz.Web.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<BackendApiOptions>()
    .Bind(builder.Configuration.GetSection(BackendApiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "BackendApi:BaseUrl must be an absolute URI.")
    .ValidateOnStart();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TasteBudz.Mvc.Session";
    options.IdleTimeout = TimeSpan.FromHours(8);
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
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Register the small MVC service layer as concrete classes.
// ASP.NET Core DI reads these registrations, creates the classes automatically for each request,
// and injects them into controller constructors when a controller asks for them.
// Example:
//   public AccountController(AuthApiService authApiService, UserSessionService userSessionService)
// The framework sees those constructor parameters and supplies the matching services from this list.
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<BackendHttpClient>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProfileApiService>();

// Register one named HttpClient for all backend calls.
// BackendHttpClient asks IHttpClientFactory for this named client whenever it needs to call the API.
builder.Services.AddHttpClient("BackendApi", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
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
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
