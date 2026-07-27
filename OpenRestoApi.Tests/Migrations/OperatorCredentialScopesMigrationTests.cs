using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public sealed class OperatorCredentialScopesMigrationTests : IDisposable
{
    private const string LastMigrationBeforeOperatorCredentialScopes = "20260727143636_AddAdminCredentialManagementAudit";

    private readonly SqliteConnection _connection;

    public OperatorCredentialScopesMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task FreshInstall_CreatesOperatorAgentCredentialScopesTable()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        Assert.Equal("table", await GetObjectTypeAsync("OperatorAgentCredentialScopes"));

        List<(string Name, string Type, long NotNull)> columns = await GetTableInfoAsync("OperatorAgentCredentialScopes");
        Assert.Contains(columns, c => c.Name == "OperatorAgentCredentialId" && c.Type == "INTEGER" && c.NotNull == 1);
        Assert.Contains(columns, c => c.Name == "RestaurantId" && c.Type == "INTEGER" && c.NotNull == 1);
        Assert.Contains(columns, c => c.Name == "CreatedAt" && c.Type == "TEXT" && c.NotNull == 1);
    }

    [Fact]
    public async Task Upgrade_CreatesSameOperatorAgentCredentialScopesSchema_AsFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeOperatorCredentialScopes);
        await migrator.MigrateAsync();

        Assert.Equal(
            GetTableSchema(freshConnection, "OperatorAgentCredentialScopes"),
            GetTableSchema(_connection, "OperatorAgentCredentialScopes"));
    }

    [Fact]
    public async Task Upgrade_BackfillsExistingCredentialScopes_FromPrincipalScopes()
    {
        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeOperatorCredentialScopes);

        Restaurant restaurant = new()
        {
            Name = "Scope Upgrade",
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
        };
        upgradeDb.Restaurants.Add(restaurant);

        OperatorPrincipal principal = new()
        {
            Identifier = "upgrade@test.com",
            NormalizedIdentifier = "upgrade@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RestaurantScopes =
            [
                new OperatorRestaurantScope
                {
                    Restaurant = restaurant,
                    CreatedAt = DateTime.UtcNow,
                },
            ],
        };
        upgradeDb.OperatorPrincipals.Add(principal);
        await upgradeDb.SaveChangesAsync();

        upgradeDb.OperatorAgentCredentials.Add(new OperatorAgentCredential
        {
            OperatorPrincipalId = principal.Id,
            CredentialKeyId = "legacycred",
            TokenDigest = new string('a', 64),
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        await upgradeDb.SaveChangesAsync();

        await migrator.MigrateAsync();

        OperatorAgentCredentialScope scope = await upgradeDb.OperatorAgentCredentialScopes.SingleAsync();
        Assert.Equal(restaurant.Id, scope.RestaurantId);
    }

    private async Task<string?> GetObjectTypeAsync(string name)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT type FROM sqlite_master WHERE name = $name;";
        cmd.Parameters.AddWithValue("$name", name);
        return (string?)await cmd.ExecuteScalarAsync();
    }

    private async Task<List<(string Name, string Type, long NotNull)>> GetTableInfoAsync(string tableName)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await cmd.ExecuteReaderAsync();

        var rows = new List<(string Name, string Type, long NotNull)>();
        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
        }

        return rows;
    }

    private static string GetTableSchema(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name;";
        cmd.Parameters.AddWithValue("$name", tableName);
        return (string)(cmd.ExecuteScalar() ?? throw new InvalidOperationException($"Table {tableName} not found."));
    }
}
