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

        int restaurantId = await InsertLegacyRestaurantAsync("Scope Upgrade");
        int principalId = await InsertLegacyOperatorPrincipalAsync("upgrade@test.com");
        await upgradeDb.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OperatorRestaurantScopes" ("OperatorPrincipalId", "RestaurantId", "CreatedAt")
            VALUES ({principalId}, {restaurantId}, {DateTime.UtcNow});
            """);

        DateTime issuedAt = DateTime.UtcNow;
        DateTime expiresAt = issuedAt.AddHours(1);
        await upgradeDb.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OperatorAgentCredentials" ("OperatorPrincipalId", "CredentialKeyId", "TokenDigest", "IssuedAt", "ExpiresAt")
            VALUES ({principalId}, {"legacycred"}, {new string('a', 64)}, {issuedAt}, {expiresAt});
            """);

        await migrator.MigrateAsync();

        OperatorAgentCredentialScope scope = await upgradeDb.OperatorAgentCredentialScopes.SingleAsync();
        Assert.Equal(restaurantId, scope.RestaurantId);
    }

    private async Task<int> InsertLegacyRestaurantAsync(string name)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "Restaurants" (
                "Name", "OpenTime", "CloseTime", "OpenDays", "Timezone",
                "IsArchived", "WalkInOnly", "DefaultBookingDurationMinutes", "BookingSlotIntervalMinutes")
            VALUES ($name, '11:00', '22:00', '1,2,3,4,5,6,7', 'UTC', 0, 0, 60, 30);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32((long)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Restaurant insert failed.")));
    }

    private async Task<int> InsertLegacyOperatorPrincipalAsync(string identifier)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "OperatorPrincipals" ("Identifier", "NormalizedIdentifier", "IsActive", "CreatedAt")
            VALUES ($identifier, $identifier, 1, $createdAt);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$identifier", identifier);
        cmd.Parameters.AddWithValue("$createdAt", DateTime.UtcNow);
        return Convert.ToInt32((long)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Principal insert failed.")));
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
