using System.Net;
using System.Net.Http.Json;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Tests.Integration;

public class RoleAuthorizationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task BookingViewer_Can_Read_Bookings_But_Cannot_Create_Them()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.BookingViewer);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/bookings")).StatusCode);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/bookings")
        {
            Content = JsonContent.Create(new { })
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task BookingEditor_Can_Mutate_Bookings_But_Cannot_Access_Admin_Overview()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/bookings")
        {
            Content = JsonContent.Create(new { })
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/overview")).StatusCode);
    }
}
