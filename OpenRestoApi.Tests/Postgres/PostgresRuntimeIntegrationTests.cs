using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Postgres;

[Collection(PostgresIntegrationCollection.Name)]
public sealed class PostgresRuntimeIntegrationTests(PostgresTestHarness harness)
{
    private readonly PostgresTestHarness _harness = harness;

    [Fact]
    public async Task RuntimeRole_CannotRunDdl_ButAppStartsAndBookingFlowSucceeds()
    {
        if (!_harness.IsEnabled)
        {
            return;
        }

        await using PostgresTestDatabase database = await _harness.CreateDatabaseAsync(nameof(RuntimeRole_CannotRunDdl_ButAppStartsAndBookingFlowSucceeds), seedApplicationData: true);

        await using (var connection = new NpgsqlConnection(database.RuntimeConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE runtime_role_should_fail(id integer);";
            await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        }

        using var factory = new PostgresTestWebAppFactory(database.RuntimeConnectionString);
        HttpClient healthClient = factory.CreateClient();
        HttpResponseMessage healthResponse = await healthClient.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        HttpClient client = factory.CreateAuthenticatedClient();
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int restaurantId = db.Restaurants.OrderBy(x => x.Id).Select(x => x.Id).First();
        int sectionId = db.Sections.OrderBy(x => x.Id).Select(x => x.Id).First();
        int tableId = db.Tables.OrderBy(x => x.Id).Select(x => x.Id).First();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/admin/bookings", new
        {
            restaurantId,
            sectionId,
            tableId,
            date = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss"),
            customerEmail = "postgres-runtime@test.com",
            seats = 2,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }
}
