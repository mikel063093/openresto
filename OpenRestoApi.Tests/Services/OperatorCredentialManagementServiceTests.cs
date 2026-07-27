using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

        SeedAdmin(db, "  Boss@Test.com ");

        var management = new OperatorCredentialManagementService(
            db,
            new OperatorCredentialService(db),
            new StubAdminActorAccessor("  Boss@Test.com ", 17));

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

        AdminCredentialManagementAudit audit = await db.AdminCredentialManagementAudits.SingleAsync();
        Assert.Equal("ISSUE", audit.Action);
        Assert.Equal(17, audit.ActorAdminCredentialId);
        Assert.Equal("boss@test.com", audit.ActorEmailSnapshot);
        Assert.Equal(stored.Id, audit.OperatorAgentCredentialId);
        Assert.Equal(stored.CredentialKeyId, audit.CredentialKeyIdSnapshot);
        Assert.Equal("operator@test.com", audit.TargetOperatorIdentifierSnapshot);
        Assert.Equal("7", audit.ScopeRestaurantIdsSnapshot);
        Assert.Equal(8, audit.TtlHoursSnapshot);
        AssertAuditDoesNotContainSecret(audit, issued.PlaintextToken);
        AssertAuditDoesNotContainSecret(audit, stored.TokenDigest);

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

        var management = new OperatorCredentialManagementService(
            db,
            new OperatorCredentialService(db),
            new StubAdminActorAccessor("boss@test.com", 17));

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
        SeedAdmin(db, "boss@test.com");

        var management = new OperatorCredentialManagementService(
            db,
            credentialService,
            new StubAdminActorAccessor("boss@test.com", 17));
        var issued = await management.IssueAsync(new IssueOperatorCredentialRequestDto
        {
            Identifier = "operator@test.com",
            RestaurantIds = new List<int> { 7 },
        }, DateTime.UtcNow);

        Assert.NotNull(await credentialService.ValidateAsync(issued.PlaintextToken));

        await management.RevokeAsync(issued.CredentialId, DateTime.UtcNow.AddMinutes(5));

        Assert.Null(await credentialService.ValidateAsync(issued.PlaintextToken, DateTime.UtcNow.AddMinutes(6)));

        List<AdminCredentialManagementAudit> audits = await db.AdminCredentialManagementAudits
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, audits.Count);
        Assert.Equal("ISSUE", audits[0].Action);
        Assert.Equal("REVOKE", audits[1].Action);
        Assert.Equal(issued.CredentialId, audits[1].OperatorAgentCredentialId);
        Assert.Equal("operator@test.com", audits[1].TargetOperatorIdentifierSnapshot);
        Assert.Equal("7", audits[1].ScopeRestaurantIdsSnapshot);
        Assert.Equal(8, audits[1].TtlHoursSnapshot);
    }

    [Fact]
    public async Task IssueAsync_PreservesExistingCredentialScope_WhenLaterCredentialForSameOperatorHasDifferentScope()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(IssueAsync_PreservesExistingCredentialScope_WhenLaterCredentialForSameOperatorHasDifferentScope));
        SeedRestaurant(db, 7, "Centro");
        SeedRestaurant(db, 8, "Patio");
        SeedAdmin(db, "boss@test.com");

        var credentialService = new OperatorCredentialService(db);
        var management = new OperatorCredentialManagementService(
            db,
            credentialService,
            new StubAdminActorAccessor("boss@test.com", 17));

        IssueOperatorCredentialResponseDto first = await management.IssueAsync(new IssueOperatorCredentialRequestDto
        {
            Identifier = "operator@test.com",
            RestaurantIds = new List<int> { 7 },
            TtlHours = 4,
            Notes = "Centro"
        }, DateTime.UtcNow);

        IssueOperatorCredentialResponseDto second = await management.IssueAsync(new IssueOperatorCredentialRequestDto
        {
            Identifier = "operator@test.com",
            RestaurantIds = new List<int> { 8 },
            TtlHours = 6,
            Notes = "Patio"
        }, DateTime.UtcNow.AddMinutes(1));

        OperatorCredentialValidationResult firstValidation = (await credentialService.ValidateAsync(first.PlaintextToken))!;
        OperatorCredentialValidationResult secondValidation = (await credentialService.ValidateAsync(second.PlaintextToken))!;

        Assert.Equal([7], firstValidation.RestaurantIds);
        Assert.Equal([8], secondValidation.RestaurantIds);

        IReadOnlyList<OperatorCredentialListItemDto> listed = await management.ListAsync();
        OperatorCredentialListItemDto firstListed = Assert.Single(listed.Where(x => x.CredentialId == first.CredentialId));
        OperatorCredentialListItemDto secondListed = Assert.Single(listed.Where(x => x.CredentialId == second.CredentialId));
        Assert.Equal([7], firstListed.Restaurants.Select(x => x.RestaurantId));
        Assert.Equal([8], secondListed.Restaurants.Select(x => x.RestaurantId));

        List<AdminCredentialManagementAudit> audits = await db.AdminCredentialManagementAudits
            .OrderBy(x => x.Id)
            .ToListAsync();
        Assert.Equal(["7", "8"], audits.Select(x => x.ScopeRestaurantIdsSnapshot));
    }

    [Fact]
    public async Task IssueAsync_RollsBackCredential_WhenAuditPersistenceFails()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new ThrowOnAuditSaveInterceptor())
            .Options;

        using AppDbContext db = new(options);
        await db.Database.EnsureCreatedAsync();

        SeedRestaurant(db, 7, "Centro");
        SeedAdmin(db, "boss@test.com");

        var management = new OperatorCredentialManagementService(
            db,
            new OperatorCredentialService(db),
            new StubAdminActorAccessor("boss@test.com", 17));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            management.IssueAsync(new IssueOperatorCredentialRequestDto
            {
                Identifier = "operator@test.com",
                RestaurantIds = new List<int> { 7 },
                TtlHours = 6
            }, DateTime.UtcNow));

        Assert.Empty(db.OperatorAgentCredentials);
        Assert.Empty(db.AdminCredentialManagementAudits);
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

    private static void SeedAdmin(AppDbContext db, string email)
    {
        db.AdminCredentials.Add(new AdminCredential
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Role = AdminRole.SuperAdmin,
            IsActive = true
        });
        db.SaveChanges();
    }

    private sealed class StubAdminActorAccessor(string email, int credentialId) : IAdminActorAccessor
    {
        public Task<AdminActorSnapshot> GetRequiredSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdminActorSnapshot(email.Trim().ToLowerInvariant(), credentialId));
    }

    private sealed class ThrowOnAuditSaveInterceptor : SaveChangesInterceptor
    {
        private static void ThrowIfAuditPending(DbContextEventData eventData)
        {
            if (eventData.Context?.ChangeTracker.Entries<AdminCredentialManagementAudit>()
                .Any(x => x.State == EntityState.Added) == true)
            {
                throw new InvalidOperationException("Simulated audit persistence failure.");
            }
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ThrowIfAuditPending(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfAuditPending(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private static void AssertAuditDoesNotContainSecret(AdminCredentialManagementAudit audit, string secret)
    {
        IEnumerable<string> persistedStrings = typeof(AdminCredentialManagementAudit)
            .GetProperties()
            .Where(x => x.PropertyType == typeof(string))
            .Select(x => (string?)x.GetValue(audit))
            .Where(x => !string.IsNullOrEmpty(x))!
            .Cast<string>();

        Assert.DoesNotContain(secret, string.Join("|", persistedStrings), StringComparison.Ordinal);
    }
}
