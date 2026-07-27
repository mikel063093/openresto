using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Services;

public sealed class OperatorCredentialServiceTests
{
    [Fact]
    public async Task IssueAndValidateAsync_ReturnsOpaqueToken_AndResolvedScope()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(IssueAndValidateAsync_ReturnsOpaqueToken_AndResolvedScope));
        SeedOperator(db, isActive: true, expiresAt: DateTime.UtcNow.AddHours(1), revokedAt: null);

        var service = new OperatorCredentialService(db);

        IssuedOperatorCredential issued = await service.IssueAsync(1, TimeSpan.FromHours(8), "test");
        OperatorCredentialValidationResult? validated = await service.ValidateAsync(issued.PlaintextToken);

        Assert.NotNull(validated);
        Assert.StartsWith("ormcp.", issued.PlaintextToken, StringComparison.Ordinal);
        Assert.NotEqual(issued.PlaintextToken, db.OperatorAgentCredentials.Single().TokenDigest);
        Assert.Equal(1, validated!.Operator.Id);
        Assert.Equal(issued.CredentialId, validated.Credential.Id);
        Assert.Equal([7], validated.RestaurantIds);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForRevokedExpiredOrInactiveOperator()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(ValidateAsync_ReturnsNull_ForRevokedExpiredOrInactiveOperator));
        SeedOperator(db, isActive: true, expiresAt: DateTime.UtcNow.AddHours(1), revokedAt: null);

        var service = new OperatorCredentialService(db);
        IssuedOperatorCredential issued = await service.IssueAsync(1, TimeSpan.FromHours(8));

        OperatorAgentCredential credential = db.OperatorAgentCredentials.Single();

        credential.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.Null(await service.ValidateAsync(issued.PlaintextToken));

        credential.RevokedAt = null;
        credential.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        Assert.Null(await service.ValidateAsync(issued.PlaintextToken));

        credential.ExpiresAt = DateTime.UtcNow.AddHours(1);
        db.OperatorPrincipals.Single().IsActive = false;
        await db.SaveChangesAsync();
        Assert.Null(await service.ValidateAsync(issued.PlaintextToken));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForUnknownRestaurantScope()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(ValidateAsync_ReturnsNull_ForUnknownRestaurantScope));
        SeedOperator(db, isActive: true, expiresAt: DateTime.UtcNow.AddHours(1), revokedAt: null);

        var service = new OperatorCredentialService(db);
        IssuedOperatorCredential issued = await service.IssueAsync(1, TimeSpan.FromHours(8));
        OperatorCredentialValidationResult? validated = await service.ValidateAsync(issued.PlaintextToken);

        Assert.NotNull(validated);
        Assert.DoesNotContain(999, validated!.RestaurantIds);
    }

    private static void SeedOperator(AppDbContext db, bool isActive, DateTime expiresAt, DateTime? revokedAt)
    {
        db.Restaurants.Add(new Restaurant { Id = 7, Name = "Scope Test" });
        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Id = 1,
            Identifier = "op@example.com",
            NormalizedIdentifier = "op@example.com",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        });
        db.OperatorRestaurantScopes.Add(new OperatorRestaurantScope
        {
            Id = 1,
            OperatorPrincipalId = 1,
            RestaurantId = 7,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
    }
}
