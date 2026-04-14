// Bootstraps the ASP.NET Core host, shared middleware pipeline, and API endpoints.
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Infrastructure.Configuration;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Centralize API failures so controllers and services can throw domain-focused exceptions.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("TasteBudzCors", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray() ?? [];

        if (origins.Length > 0)
        {
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register the modular backend services and shared infrastructure used by the MVP backend.
builder.Services.AddTasteBudzFoundation(builder.Configuration);

var app = builder.Build();
var normalizedConnectionString = SqliteConnectionStringHelper.Normalize(
    builder.Configuration.GetConnectionString("TasteBudz") ?? throw new InvalidOperationException("ConnectionStrings:TasteBudz must be configured."),
    app.Environment.ContentRootPath);
var persistenceOptions = app.Services.GetRequiredService<IOptions<PersistenceOptions>>().Value;
await SqliteDatabaseBootstrapper.EnsureInitializedAsync(
    normalizedConnectionString,
    persistenceOptions.InitializeSqliteOnStartup,
    persistenceOptions.SeedTestDataOnStartup,
    app.Environment.EnvironmentName,
    app.Logger,
    app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("TasteBudzCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireCors("TasteBudzCors");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;