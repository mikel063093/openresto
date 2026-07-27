using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class OperatorReservationOwnershipIntegrationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task OwnerCanCreateListGetAndCancel_ButSecondOperatorCannotReadOrCancel()
    {
        (int restaurantId, int sectionId, int tableId) ids = await SeedRestaurantGraphAsync();
        string ownerToken = await SeedOperatorTokenAsync("owner@test.com", ids.restaurantId);
        string otherToken = await SeedOperatorTokenAsync("other@test.com", ids.restaurantId);

        HttpClient ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        string bookingDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddT12:00:00");
        HttpResponseMessage createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/internal/operators/restaurants/{ids.restaurantId}/reservations",
            new
            {
                restaurantId = ids.restaurantId,
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate,
                customerEmail = "guest@example.com",
                customerName = "Guest",
                seats = 2,
                specialRequests = "Window"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        BookingDto? created = await createResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(created);

        HttpResponseMessage listResponse = await ownerClient.GetAsync("/api/internal/operators/reservations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        List<BookingDto>? listed = await listResponse.Content.ReadFromJsonAsync<List<BookingDto>>();
        Assert.NotNull(listed);
        Assert.Contains(listed!, x => x.Id == created!.Id);

        HttpResponseMessage getResponse = await ownerClient.GetAsync($"/api/internal/operators/reservations/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        HttpClient otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await otherClient.GetAsync($"/api/internal/operators/reservations/{created.Id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await otherClient.PostAsync($"/api/internal/operators/reservations/{created.Id}/cancel", null)).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await ownerClient.PostAsync($"/api/internal/operators/reservations/{created.Id}/cancel", null)).StatusCode);

        using IServiceScope auditScope = _factory.Services.CreateScope();
        AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(auditDb.OperatorActionAudits.ToList(), x => x.BookingId == created.Id && x.Action == "reservation.cancel");
    }

    [Fact]
    public async Task OwnerCanUpdateAndEscalate_ButForeignOperatorGetsNotFound()
    {
        (int restaurantId, int sectionId, int tableId) ids = await SeedRestaurantGraphAsync();
        string ownerToken = await SeedOperatorTokenAsync("owner-update@test.com", ids.restaurantId);
        string otherToken = await SeedOperatorTokenAsync("other-update@test.com", ids.restaurantId);

        HttpClient ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        DateTime bookingDate = DateTime.UtcNow.AddDays(6).Date.AddHours(12);
        BookingDto created = (await (await ownerClient.PostAsJsonAsync(
            $"/api/internal/operators/restaurants/{ids.restaurantId}/reservations",
            new
            {
                restaurantId = ids.restaurantId,
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate,
                customerEmail = "guest2@example.com",
                customerName = "Guest 2",
                seats = 2
            })).Content.ReadFromJsonAsync<BookingDto>())!;

        HttpResponseMessage updateResponse = await ownerClient.PutAsJsonAsync(
            $"/api/internal/operators/reservations/{created.Id}",
            new
            {
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate.AddHours(1),
                customerEmail = "guest2@example.com",
                customerName = "Updated Name",
                seats = 3,
                specialRequests = "Birthday"
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage escalationResponse = await ownerClient.PostAsJsonAsync(
            $"/api/internal/operators/reservations/{created.Id}/escalate",
            new { reason = "Guest requested manager follow-up" });
        Assert.Equal(HttpStatusCode.OK, escalationResponse.StatusCode);

        HttpClient otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PutAsJsonAsync(
            $"/api/internal/operators/reservations/{created.Id}",
            new
            {
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate.AddHours(2),
                customerEmail = "guest2@example.com",
                customerName = "Other",
                seats = 2
            })).StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(db.OperatorActionAudits.ToList(), x => x.BookingId == created.Id && x.Action == "reservation.update" && x.Outcome == "success");
        Assert.Contains(db.OperatorActionAudits.ToList(), x => x.BookingId == created.Id && x.Action == "reservation.escalate" && x.Outcome == "success");
    }

    private async Task<(int restaurantId, int sectionId, int tableId)> SeedRestaurantGraphAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = "Ownership Test",
            OpenTime = "11:00",
            CloseTime = "13:00",
            Timezone = "UTC",
            Sections = new List<Section>
            {
                new()
                {
                    Name = "Main",
                    Tables = new List<Table> { new() { Name = "T1", Seats = 4 } }
                }
            }
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        Section section = restaurant.Sections.Single();
        Table table = section.Tables.Single();
        return (restaurant.Id, section.Id, table.Id);
    }

    private async Task<string> SeedOperatorTokenAsync(string email, int scopedRestaurantId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Identifier = email,
            NormalizedIdentifier = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RestaurantScopes = new List<OperatorRestaurantScope>
            {
                new()
                {
                    RestaurantId = scopedRestaurantId,
                    CreatedAt = DateTime.UtcNow,
                }
            }
        });
        await db.SaveChangesAsync();

        var service = new OperatorCredentialService(db);
        int operatorId = db.OperatorPrincipals.Single(x => x.NormalizedIdentifier == email).Id;
        IssuedOperatorCredential issued = await service.IssueAsync(operatorId, TimeSpan.FromHours(1));
        return issued.PlaintextToken;
    }
}
