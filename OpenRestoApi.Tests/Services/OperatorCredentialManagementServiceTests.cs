using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Services;

public sealed class OperatorCredentialManagementServiceTests
{
    [Fact]
    public async Task IssueAsync_NormalizesIdentifier_AndPersistsNonSecretMetadataOnly()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(IssueAsync_NormalizesIdentifier_AndPersistsNonSecretMetadataOnly));
        SeedRestaurant(db, 7, "Centro");

        var management = new OperatorCredentialManagementService(db, new OperatorCredentialService(db));

        var issued = await management.IssueAsync(new IssueOperatorCredentialRequestDto
        {
            Identifier = "  OPERATOR@Test.com ",
            RestaurantIds = new List<int> { 7 },
            TtlHours = 8,
            Notes = "Soporte"
        }, DateTime.UtcNow);

        Assert.Equal("operator@test.com", issued.Identifier);
        Assert.StartsWith("ormcp.", issued.PlaintextToken, StringComparison.Ordinal);

        OperatorAgentCredential stored = await db.OperatorAgentCredentials.SingleAsync();
        Assert.NotEqual(issued.PlaintextToken, stored.TokenDigest);

        OperatorPrincipal principal = await db.OperatorPrincipals.Include(x => x.RestaurantScopes).SingleAsync();
        Assert.Equal("operator@test.com", principal.NormalizedIdentifier);
        Assert.Single(principal.RestaurantScopes);

        IReadOnlyList<OperatorCredentialListItemDto> list = await management.ListAsync();
        Assert.Single(list);
        Assert.Equal("operator@test.com", list[0].Identifier);
        Assert.Equal("Soporte", list[0].Notes);
        Assert.Single(list[0].Restaurants);
    }

    [Fact]
    public async Task IssueAsync_Rejects_InvalidScope_And_BoundedFields()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(IssueAsync_Rejects_InvalidScope_And_BoundedFields));
        SeedRestaurant(db, 7, "Centro");

        var management = new OperatorCredentialManagementService(db, new OperatorCredentialService(db));

        await Assert.ThrowsAsync<OpenRestoApi.Core.Application.Exceptions.ValidationException>(() =>
            management.IssueAsync(new IssueOperatorCredentialRequestDto
            {
                Identifier = "operator@test.com",
                RestaurantIds = new List<int>(),
            }, DateTime.UtcNow));

        await Assert.ThrowsAsync<OpenRestoApi.Core.Application.Exceptions.ValidationException>(() =>
            management.IssueAsync(new IssueOperatorCredentialRequestDto
            {
                Identifier = "operator@test.com",
                RestaurantIds = new List<int> { 999 },
            }, DateTime.UtcNow));

        await Assert.ThrowsAsync<OpenRestoApi.Core.Application.Exceptions.ValidationException>(() =>
            management.IssueAsync(new IssueOperatorCredentialRequestDto
            {
                Identifier = "operator@test.com",
                RestaurantIds = new List<int> { 7 },
                TtlHours = 25,
            }, DateTime.UtcNow));

        await Assert.ThrowsAsync<OpenRestoApi.Core.Application.Exceptions.ValidationException>(() =>
            management.IssueAsync(new IssueOperatorCredentialRequestDto
            {
                Identifier = "operator@test.com",
                RestaurantIds = new List<int> { 7 },
                Notes = new string('a', 201),
            }, DateTime.UtcNow));
    }

    [Fact]
    public async Task RevokeAsync_DisablesExistingCredentialValidation()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(RevokeAsync_DisablesExistingCredentialValidation));
        SeedRestaurant(db, 7, "Centro");

        var credentialService = new OperatorCredentialService(db);
        var management = new OperatorCredentialManagementService(db, credentialService);
        var issued = await management.IssueAsync(new IssueOperatorCredentialRequestDto
        {
            Identifier = "operator@test.com",
            RestaurantIds = new List<int> { 7 },
        }, DateTime.UtcNow);

        Assert.NotNull(await credentialService.ValidateAsync(issued.PlaintextToken));

        await management.RevokeAsync(issued.CredentialId, DateTime.UtcNow.AddMinutes(5));

        Assert.Null(await credentialService.ValidateAsync(issued.PlaintextToken, DateTime.UtcNow.AddMinutes(6)));
    }

    private static void SeedRestaurant(AppDbContext db, int id, string name)
    {
        db.Restaurants.Add(new Restaurant
        {
            Id = id,
            Name = name,
            OpenTime = "10:00",
            CloseTime = "22:00",
            Timezone = "UTC",
        });
        db.SaveChanges();
    }
}
