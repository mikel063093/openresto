using System.Net;
using System.Text.Json;

namespace OpenRestoApi.Tests.Integration;

public class OpenApiDocumentationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task OpenApiDocument_ContainsRoutesSchemasAndBearerScheme()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.Contains(root.GetProperty("paths").EnumerateObject(),
            path => string.Equals(path.Name, "/api/admin/bookings", StringComparison.OrdinalIgnoreCase));
        JsonElement bearer = root.GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
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
}
