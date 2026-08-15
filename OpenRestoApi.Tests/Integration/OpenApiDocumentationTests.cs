using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OpenRestoApi.Tests.Integration;

public class OpenApiDocumentationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private const string GuidePath = "/api-reference/operator-mcp";
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task OpenApiDocument_ContainsBearerSchemeAndDocumentedResponseContracts()
    {
        JsonElement root = await GetOpenApiDocumentAsync();

        Assert.Contains(root.GetProperty("paths").EnumerateObject(),
            path => string.Equals(path.Name, "/api/admin/bookings", StringComparison.OrdinalIgnoreCase));

        JsonElement bearer = root.GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        AssertJsonResponse(root, "/api/restaurants/{id}", "get", "200");
        AssertJsonResponse(root, "/api/restaurants/{id}", "get", "404", expectedSchemaNames: ["ProblemDetails"]);

        AssertJsonResponse(root, "/api/bookings", "post", "201", expectedSchemaNames: ["BookingDto"]);
        AssertJsonResponse(root, "/api/bookings", "post", "400",
            expectedSchemaNames: ["ValidationProblemDetails"]);
        AssertJsonResponse(root, "/api/bookings", "post", "409", expectedSchemaNames: ["MessageResponse"]);

        AssertResponseExists(root, "/api/holds/{holdId}", "delete", "204");

        AssertJsonResponse(root, "/api/admin/bookings", "get", "200");
        AssertResponseExists(root, "/api/admin/bookings", "get", "401");
        AssertResponseExists(root, "/api/admin/bookings", "get", "403");

        AssertJsonResponse(root, "/api/admin/auth/me", "get", "200", expectedSchemaNames: ["AuthIdentityResponse"]);
        AssertJsonResponse(root, "/api/admin/auth/me", "get", "401", expectedSchemaNames: ["ProblemDetails"]);

        AssertJsonResponse(root, "/api/restaurants/{restaurantId}/availability", "get", "404",
            expectedSchemaNames: ["MessageResponse"]);
        AssertJsonResponse(root, "/api/bookings/{id}", "get", "404", expectedSchemaNames: ["ProblemDetails"]);
        AssertJsonResponse(root, "/api/holds", "post", "400",
            expectedSchemaNames: ["MessageResponse", "ValidationProblemDetails"]);
        AssertJsonResponse(root, "/api/admin/auth/change-email", "post", "400",
            expectedSchemaNames: ["MessageResponse", "ValidationProblemDetails"]);

        string description = root.GetProperty("info").GetProperty("description").GetString() ?? string.Empty;
        Assert.Contains("Streamable HTTP / JSON-RPC", description, StringComparison.Ordinal);
        Assert.Contains("POST /api/mcp/operator", description, StringComparison.Ordinal);
        Assert.Contains(GuidePath, description, StringComparison.Ordinal);
        Assert.Contains("must not be exercised through REST \"Try it\"", description, StringComparison.Ordinal);
        AssertEveryOperationHasDocumentedResponseBodies(root);
    }

    [Fact]
    public async Task ApiReference_LoadsInteractiveScalarHtml()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api-reference");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Scalar", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OperatorMcpGuide_LoadsProtectedGuideContent_WithoutLiveSecrets()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync(GuidePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("OpenResto Operator MCP Guide", html, StringComparison.Ordinal);
        Assert.Contains("Streamable HTTP / JSON-RPC", html, StringComparison.Ordinal);
        Assert.Contains("POST /api/mcp/operator", html, StringComparison.Ordinal);
        Assert.Contains("OPENRESTO_MCP_TOKEN", html, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", html, StringComparison.Ordinal);
        Assert.Contains("401 Unauthorized", html, StringComparison.Ordinal);
        Assert.Contains("404 Not Found", html, StringComparison.Ordinal);
        Assert.DoesNotContain("guest@example.com", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductionDocsExposure_RemainsSuperAdminProtected()
    {
        using var productionFactory = new TestWebAppFactory(environmentName: "Production", exposeOpenApiDocs: true);
        HttpClient anonymous = productionFactory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api-reference")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(GuidePath)).StatusCode);

        using IServiceScope scope = productionFactory.Services.CreateScope();
        EndpointDataSource endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
        RouteEndpoint guideEndpoint = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, GuidePath, StringComparison.Ordinal)
                || string.Equals(endpoint.RoutePattern.RawText, GuidePath.TrimStart('/'), StringComparison.Ordinal));
        IAuthorizeData authorizeData = Assert.Single(guideEndpoint.Metadata.OfType<IAuthorizeData>());
        Assert.Equal("SuperAdminOnly", authorizeData.Policy);
        Assert.Equal("Bearer", authorizeData.AuthenticationSchemes);
    }
    private async Task<JsonElement> GetOpenApiDocumentAsync()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static void AssertEveryOperationHasDocumentedResponseBodies(JsonElement root)
    {
        foreach (JsonProperty path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (JsonProperty operation in path.Value.EnumerateObject())
            {
                if (!IsHttpMethod(operation.Name))
                {
                    continue;
                }

                JsonElement responses = operation.Value.GetProperty("responses");
                Assert.True(responses.EnumerateObject().Any(),
                    $"Expected {operation.Name.ToUpperInvariant()} {path.Name} to document at least one response.");

                foreach (JsonProperty response in responses.EnumerateObject().Where(response => response.Name != "204"))
                {
                    if (!response.Value.TryGetProperty("content", out JsonElement content)
                        || !content.EnumerateObject().Any())
                    {
                        string description = response.Value.GetProperty("description").GetString() ?? string.Empty;
                        Assert.True(description.Contains("no response body", StringComparison.OrdinalIgnoreCase),
                            $"Expected {operation.Name.ToUpperInvariant()} {path.Name} {response.Name} without content to explicitly document no response body; actual: {description}");
                        continue;
                    }

                    foreach (JsonProperty mediaType in content.EnumerateObject())
                    {
                        Assert.True(mediaType.Value.TryGetProperty("schema", out _),
                            $"Expected {operation.Name.ToUpperInvariant()} {path.Name} {response.Name} {mediaType.Name} to document a schema.");
                    }

                    if (content.TryGetProperty("application/json", out JsonElement jsonMediaType))
                    {
                        Assert.True(ResponseHasExample(root, jsonMediaType),
                            $"Expected {operation.Name.ToUpperInvariant()} {path.Name} {response.Name} to include a JSON example.");
                    }
                }
            }
        }
    }

    private static void AssertResponseExists(JsonElement root, string path, string method, string statusCode)
    {
        JsonElement responses = GetPathItem(root, path)
            .GetProperty(method)
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _),
            $"Expected {method.ToUpperInvariant()} {path} to document HTTP {statusCode}.");
    }

    private static void AssertJsonResponse(
        JsonElement root,
        string path,
        string method,
        string statusCode,
        params string[] expectedSchemaNames)
    {
        AssertResponseExists(root, path, method, statusCode);

        JsonElement response = GetPathItem(root, path)
            .GetProperty(method)
            .GetProperty("responses")
            .GetProperty(statusCode);

        JsonElement mediaType = response.GetProperty("content").GetProperty("application/json");
        JsonElement schema = mediaType.GetProperty("schema");
        HashSet<string> schemaNames = GetSchemaNames(schema);

        foreach (string schemaName in expectedSchemaNames)
        {
            Assert.True(schemaNames.Contains(schemaName) || SchemaContainsShape(schema, schemaName),
                $"Expected schema {schemaName}. Actual schema: {schema.GetRawText()}");
        }

        Assert.True(ResponseHasExample(root, mediaType),
            $"Expected {method.ToUpperInvariant()} {path} {statusCode} response to include a JSON example.");
    }

    private static HashSet<string> GetSchemaNames(JsonElement schema)
    {
        var schemaNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (schema.TryGetProperty("$ref", out JsonElement schemaRef))
        {
            schemaNames.Add(GetRefName(schemaRef.GetString()!));
        }

        if (schema.TryGetProperty("items", out JsonElement items))
        {
            foreach (string itemSchemaName in GetSchemaNames(items))
            {
                schemaNames.Add(itemSchemaName);
            }
        }

        if (schema.TryGetProperty("oneOf", out JsonElement oneOf))
        {
            foreach (JsonElement child in oneOf.EnumerateArray())
            {
                foreach (string childSchemaName in GetSchemaNames(child))
                {
                    schemaNames.Add(childSchemaName);
                }
            }
        }

        if (schema.TryGetProperty("anyOf", out JsonElement anyOf))
        {
            foreach (JsonElement child in anyOf.EnumerateArray())
            {
                foreach (string childSchemaName in GetSchemaNames(child))
                {
                    schemaNames.Add(childSchemaName);
                }
            }
        }

        return schemaNames;
    }

    private static bool SchemaContainsShape(JsonElement schema, string expectedSchemaName)
    {
        string requiredProperty = expectedSchemaName switch
        {
            "MessageResponse" => "message",
            "ProblemDetails" => "title",
            "ValidationProblemDetails" => "errors",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(requiredProperty))
        {
            return false;
        }

        if (schema.TryGetProperty("properties", out JsonElement properties)
            && properties.TryGetProperty(requiredProperty, out _))
        {
            return true;
        }

        foreach (string composition in new[] { "oneOf", "allOf", "anyOf" })
        {
            if (schema.TryGetProperty(composition, out JsonElement children)
                && children.ValueKind == JsonValueKind.Array
                && children.EnumerateArray().Any(child => SchemaContainsShape(child, expectedSchemaName)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ResponseHasExample(JsonElement root, JsonElement mediaType)
    {
        if (mediaType.TryGetProperty("example", out _))
        {
            return true;
        }

        if (!mediaType.TryGetProperty("schema", out JsonElement schema))
        {
            return false;
        }

        return SchemaHasExample(root, schema);
    }

    private static bool SchemaHasExample(JsonElement root, JsonElement schema)
    {
        if (schema.TryGetProperty("example", out _))
        {
            return true;
        }

        if (schema.TryGetProperty("$ref", out JsonElement schemaRef))
        {
            string schemaName = GetRefName(schemaRef.GetString()!);
            JsonElement componentSchema = root.GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(schemaName);

            if (componentSchema.TryGetProperty("example", out _))
            {
                return true;
            }

            if (componentSchema.TryGetProperty("items", out JsonElement componentItems)
                && SchemaHasExample(root, componentItems))
            {
                return true;
            }

            if (componentSchema.TryGetProperty("oneOf", out JsonElement componentOneOf))
            {
                return componentOneOf.EnumerateArray().Any(option => SchemaHasExample(root, option));
            }
        }

        if (schema.TryGetProperty("items", out JsonElement items))
        {
            return SchemaHasExample(root, items);
        }

        if (schema.TryGetProperty("oneOf", out JsonElement oneOf))
        {
            return oneOf.EnumerateArray().Any(option => SchemaHasExample(root, option));
        }

        return false;
    }

    private static string GetRefName(string reference)
        => reference[(reference.LastIndexOf('/') + 1)..];

    private static JsonElement GetPathItem(JsonElement root, string path)
    {
        foreach (JsonProperty candidate in root.GetProperty("paths").EnumerateObject())
        {
            if (string.Equals(candidate.Name, path, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value;
            }
        }

        throw new Xunit.Sdk.XunitException($"Expected path '{path}' to exist in the generated OpenAPI document.");
    }

    private static bool IsHttpMethod(string name)
        => name is "get" or "post" or "put" or "patch" or "delete" or "head" or "options" or "trace";
}
