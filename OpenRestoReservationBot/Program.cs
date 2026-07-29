using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenRestoReservationBot.Infrastructure;
using OpenRestoReservationBot.Options;
using OpenRestoReservationBot.Security;
using OpenRestoReservationBot.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
}

builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BotExceptionHandler>();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddOptions<ReservationBotOptions>()
    .Bind(builder.Configuration.GetSection(ReservationBotOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.InternalCredential), "ReservationBot:InternalCredential es obligatorio.")
    .Validate(options => Uri.TryCreate(options.OpenResto.AvailabilityBaseUrl, UriKind.Absolute, out _), "ReservationBot:OpenResto:AvailabilityBaseUrl debe ser una URL absoluta.")
    .Validate(options => Uri.TryCreate(options.OpenResto.PrivateBaseUrl, UriKind.Absolute, out _), "ReservationBot:OpenResto:PrivateBaseUrl debe ser una URL absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.OpenResto.PrivateApiCredential), "ReservationBot:OpenResto:PrivateApiCredential es obligatorio.")
    .Validate(options => options.OpenResto.TimeoutSeconds is >= 1 and <= 60, "ReservationBot:OpenResto:TimeoutSeconds debe estar entre 1 y 60.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(BotInternalAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BotInternalAuthenticationHandler>(
        BotInternalAuthenticationDefaults.SchemeName,
        _ => { });

builder.Services.AddAuthorization();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        Dictionary<string, string[]> errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "body" : entry.Key,
                entry => entry.Value!.Errors.Select(_ => "La solicitud no cumple el esquema esperado.").Distinct().ToArray(),
                StringComparer.Ordinal);

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "La solicitud es inválida.",
            Detail = "Revise el contrato del bot y envíe únicamente los campos permitidos.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problemDetails);
    };
});

builder.Services.AddSingleton<PiiRedactor>();
builder.Services.AddScoped<IBotRequestContextAccessor, BotRequestContextAccessor>();
builder.Services.AddScoped<IOpenRestoAvailabilityClient, OpenRestoAvailabilityClient>();
builder.Services.AddScoped<IOpenRestoPrivateChannelClient, OpenRestoPrivateChannelClient>();
builder.Services.AddScoped<IBotOperationRouter, BotOperationRouter>();

builder.Services.AddHttpClient(OpenRestoAvailabilityClient.ClientName)
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        ReservationBotOptions options = serviceProvider.GetRequiredService<IOptions<ReservationBotOptions>>().Value;
        client.BaseAddress = new Uri(options.OpenResto.AvailabilityBaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.OpenResto.TimeoutSeconds);
    });

builder.Services.AddHttpClient(OpenRestoPrivateChannelClient.ClientName)
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        ReservationBotOptions options = serviceProvider.GetRequiredService<IOptions<ReservationBotOptions>>().Value;
        client.BaseAddress = new Uri(options.OpenResto.PrivateBaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.OpenResto.TimeoutSeconds);
    });

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallback("/api/{**catchAll}", (HttpContext ctx) =>
    Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "No encontrado",
        detail: $"La ruta interna '{ctx.Request.Path}' no existe."));

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.Run();

public partial class Program;
