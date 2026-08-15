using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Tests.Integration;

public class AdminUsersControllerTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task SuperAdmin_Can_Create_And_List_Users()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage create = await client.PostAsJsonAsync("/api/admin/users", new
        {
            email = "viewer@example.com", password = "password", role = AdminRole.BookingViewer
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        HttpResponseMessage list = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("viewer@example.com", await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SuperAdmin_Can_Create_User_When_Role_Is_A_String()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        using var request = new StringContent(
            """
            {"email":"editor@example.com","password":"password","role":"BookingEditor"}
            """,
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("/api/admin/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonNode? body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("editor@example.com", body?["email"]?.GetValue<string>());
        Assert.Equal("BookingEditor", body?["role"]?.GetValue<string>());
        Assert.True(body?["isActive"]?.GetValue<bool>());
    }

    [Fact]
    public async Task Invalid_Role_String_Returns_Clear_Validation_Error_Without_Request_Noise()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.SuperAdmin, "es-CO");
        using var request = new StringContent(
            """
            {"email":"invalid-role@example.com","password":"password","role":"Owner"}
            """,
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("/api/admin/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonNode? body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", body?["type"]?.GetValue<string>());
        Assert.False(body?["errors"]?["request"] is not null);
        Assert.Equal(
            "Rol no valido. Valores permitidos: SuperAdmin, BookingViewer, BookingEditor.",
            body?["errors"]?["role"]?[0]?.GetValue<string>());
    }

    [Theory]
    [InlineData(AdminRole.BookingViewer)]
    [InlineData(AdminRole.BookingEditor)]
    public async Task Non_SuperAdmins_Cannot_Manage_Users(AdminRole role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/admin/users", new
        {
            email = "viewer@example.com", password = "password", role = "BookingViewer"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/admin/users/1", new
        {
            role = "BookingEditor"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/admin/users/1/deactivate", null)).StatusCode);
    }
}
