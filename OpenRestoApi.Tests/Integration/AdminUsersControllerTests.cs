using System.Net;
using System.Net.Http.Json;
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
    public async Task BookingEditor_Cannot_Manage_Users()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
    }
}
