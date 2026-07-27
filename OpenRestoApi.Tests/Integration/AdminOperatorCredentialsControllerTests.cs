using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class AdminOperatorCredentialsControllerTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task SuperAdmin_Can_Issue_List_And_Revoke_OperatorCredential()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage issueResponse = await client.PostAsJsonAsync("/api/admin/operator-credentials", new
        {
            identifier = "  Operator.One@Test.com  ",
            restaurantIds = new[] { restaurantId },
            ttlHours = 6,
            notes = "Turno PM"
        });

        Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
        IssueOperatorCredentialResponse issued = (await issueResponse.Content.ReadFromJsonAsync<IssueOperatorCredentialResponse>())!;
        Assert.Equal("operator.one@test.com", issued.Identifier);
        Assert.StartsWith("ormcp.", issued.PlaintextToken, StringComparison.Ordinal);
        Assert.Equal("Turno PM", issued.Notes);

        using (IServiceScope auditScope = _factory.Services.CreateScope())
        {
            AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AdminCredentialManagementAudit issueAudit = Assert.Single(auditDb.AdminCredentialManagementAudits);
            OperatorAgentCredential persistedCredential = Assert.Single(auditDb.OperatorAgentCredentials);

            Assert.Equal("ISSUE", issueAudit.Action);
            Assert.Equal(TestWebAppFactory.AdminEmail, issueAudit.ActorEmailSnapshot);
            Assert.Equal("operator.one@test.com", issueAudit.TargetOperatorIdentifierSnapshot);
            Assert.Equal(issued.CredentialId, issueAudit.OperatorAgentCredentialId);
            Assert.Equal(persistedCredential.CredentialKeyId, issueAudit.CredentialKeyIdSnapshot);
            Assert.Equal(6, issueAudit.TtlHoursSnapshot);
            Assert.Equal(restaurantId.ToString(), issueAudit.ScopeRestaurantIdsSnapshot);
            Assert.DoesNotContain(issued.PlaintextToken, issueAudit.ActorEmailSnapshot, StringComparison.Ordinal);
            Assert.DoesNotContain(issued.PlaintextToken, issueAudit.TargetOperatorIdentifierSnapshot, StringComparison.Ordinal);
            Assert.DoesNotContain(persistedCredential.TokenDigest, issueAudit.ActorEmailSnapshot, StringComparison.Ordinal);
            Assert.DoesNotContain(persistedCredential.TokenDigest, issueAudit.TargetOperatorIdentifierSnapshot, StringComparison.Ordinal);
        }

        HttpResponseMessage listResponse = await client.GetAsync("/api/admin/operator-credentials");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        string listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("operator.one@test.com", listBody, StringComparison.Ordinal);
        Assert.DoesNotContain(issued.PlaintextToken, listBody, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenDigest", listBody, StringComparison.OrdinalIgnoreCase);

        HttpClient operatorClient = _factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.PlaintextToken);
        string date = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");
        HttpResponseMessage beforeRevoke = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{restaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.OK, beforeRevoke.StatusCode);

        HttpResponseMessage revokeResponse = await client.PostAsync(
            $"/api/admin/operator-credentials/{issued.CredentialId}/revoke",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using (IServiceScope auditScope = _factory.Services.CreateScope())
        {
            AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            List<AdminCredentialManagementAudit> audits = auditDb.AdminCredentialManagementAudits.OrderBy(x => x.Id).ToList();
            Assert.Equal(2, audits.Count);
            Assert.Equal("REVOKE", audits[1].Action);
            Assert.Equal(TestWebAppFactory.AdminEmail, audits[1].ActorEmailSnapshot);
            Assert.Equal(issued.CredentialId, audits[1].OperatorAgentCredentialId);
            Assert.Equal("operator.one@test.com", audits[1].TargetOperatorIdentifierSnapshot);
        }

        HttpResponseMessage afterRevoke = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{restaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task Issue_Rejects_MissingScope_And_TtlBeyondLimit()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage noScopeResponse = await client.PostAsJsonAsync("/api/admin/operator-credentials", new
        {
            identifier = "operator@test.com",
            restaurantIds = Array.Empty<int>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, noScopeResponse.StatusCode);

        HttpResponseMessage ttlResponse = await client.PostAsJsonAsync("/api/admin/operator-credentials", new
        {
            identifier = "operator@test.com",
            restaurantIds = new[] { restaurantId },
            ttlHours = 25
        });
        Assert.Equal(HttpStatusCode.BadRequest, ttlResponse.StatusCode);
    }

    private async Task<int> SeedRestaurantAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = $"Scoped {Guid.NewGuid():N}",
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant.Id;
    }

    private sealed class IssueOperatorCredentialResponse
    {
        public int CredentialId { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string PlaintextToken { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
