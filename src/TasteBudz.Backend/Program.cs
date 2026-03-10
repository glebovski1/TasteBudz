// Bootstraps the ASP.NET Core host, shared middleware pipeline, and API endpoints.
using System.Text.Json.Serialization;
using TasteBudz.Backend.Infrastructure.Configuration;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Centralize API failures so controllers and services can throw domain-focused exceptions.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Register the modular backend services and the in-memory infrastructure used in the MVP.
builder.Services.AddTasteBudzFoundation(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
