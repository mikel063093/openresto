using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenRestoApi.Extensions;
using OpenRestoApi.Infrastructure.Exceptions;
using OpenRestoApi.Infrastructure.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Ensure the app listens on the PORT environment variable for Railway, defaulting to 8080
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// ProblemDetails backs UseStatusCodePages (bare 404/405 etc.) and [ApiController]
// model-state validation. The typed GlobalExceptionHandler runs first for *thrown*
// exceptions (AddExceptionHandler registers it ahead of the default ProblemDetails
// handler) and owns the {message: "..."} body shape for OpenRestoException types.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProjectDependencies();
builder.Services.AddCustomCors(builder.Configuration);
builder.Services.AddCustomRateLimiting(builder.Environment);
builder.Services.AddCustomAuthentication(builder.Configuration);

DatabaseProvider databaseProvider = builder.Configuration.GetDatabaseProvider();
string connectionString = builder.Configuration.GetAppConnectionString(databaseProvider, builder.Environment);
builder.Services.AddDatabaseSetup(connectionString, databaseProvider, builder.Environment);

WebApplication app = builder.Build();

// Convert unhandled exceptions and bare status-code responses (404/405/etc.)
// into JSON. Must be one of the first middlewares so it can wrap the rest of the
// pipeline. Thrown OpenRestoException types are mapped to {message:"..."} by the
// registered GlobalExceptionHandler (above); bare status codes fall back to
// ProblemDetails via UseStatusCodePages below.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseForwardedHeaders();

bool exposeApiReference = app.Environment.IsDevelopment()
    || app.Environment.IsEnvironment("Testing")
    || builder.Configuration.GetValue<bool>("OpenApi:ExposeDocs");

if (exposeApiReference)
{
    var openApi = app.MapOpenApi();
    var apiReference = app.MapScalarApiReference("/api-reference");
    var mcpGuide = app.MapGet(
            OperatorMcpDocumentation.GuidePath,
            () => Results.Content(OperatorMcpDocumentation.HtmlPage, "text/html; charset=utf-8"))
        .ExcludeFromDescription();

    // Development and isolated test-host runs intentionally keep docs open for local tooling.
    // Any deployed non-development environment must opt in and is restricted to SuperAdmin.
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        var superAdminDocsAuth = new AuthorizeAttribute
        {
            Policy = "SuperAdminOnly",
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        };

        openApi.RequireAuthorization(superAdminDocsAuth);
        apiReference.RequireAuthorization(superAdminDocsAuth);
        mcpGuide.RequireAuthorization(superAdminDocsAuth);
    }
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
var mcpEndpoint = app.MapMcp("/api/mcp/operator")
    .RequireAuthorization(new AuthorizeAttribute
    {
        AuthenticationSchemes = OpenRestoApi.Infrastructure.Auth.OperatorAuthenticationDefaults.SchemeName,
    });

if (!app.Environment.IsEnvironment("Testing"))
{
    mcpEndpoint.RequireRateLimiting("operatorMcp");
}

// Anything under /api/* that isn't matched by a controller route returns a
// JSON ProblemDetails 404 — never the SPA's HTML index or an empty body.
app.MapFallback("/api/{**catchAll}", (HttpContext ctx) =>
    Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Not Found",
        detail: $"The requested API endpoint '{ctx.Request.Path}' does not exist."));

app.InitializeDatabase(connectionString, databaseProvider, builder.Configuration);

// Health endpoint: JSON body (consistent with the rest of the API), and opted
// out of rate limiting so liveness probes / scanners never get throttled.
app.MapGet("/api/health", () => Results.Ok(new HealthResponse { Status = "ok" }))
    .DisableRateLimiting()
    .Produces<HealthResponse>(StatusCodes.Status200OK);

app.Run();
