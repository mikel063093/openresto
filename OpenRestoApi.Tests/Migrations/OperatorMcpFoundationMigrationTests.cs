using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

/// <summary>
/// Proves the operator-MCP foundation migration creates the new operator tables and nullable
/// booking ownership columns on a fresh install, and that upgrading from the prior schema
/// produces the exact same Bookings table definition.
/// </summary>
public sealed class OperatorMcpFoundationMigrationTests : IDisposable
{
    private const string LastMigrationBeforeOperatorMcpFoundation = "20260722013508_AddAdminCredentialIsActive";

    private readonly SqliteConnection _connection;

    public OperatorMcpFoundationMigrationTests()
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
    public async Task FreshInstall_CreatesOperatorTables_AndNullableBookingOwnershipColumns()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        Assert.Equal("table", await GetObjectTypeAsync("OperatorPrincipals"));
        Assert.Equal("table", await GetObjectTypeAsync("OperatorRestaurantScopes"));
        Assert.Equal("table", await GetObjectTypeAsync("OperatorAgentCredentials"));
        Assert.Equal("table", await GetObjectTypeAsync("OperatorActionAudits"));

        var bookingColumns = await GetTableInfoAsync("Bookings");
        var credentialColumns = await GetTableInfoAsync("OperatorAgentCredentials");

        Assert.Contains(bookingColumns, c => c.Name == "CreatedByOperatorId" && c.Type == "INTEGER" && c.NotNull == 0);
        Assert.Contains(bookingColumns, c => c.Name == "CreatedViaChannel" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(credentialColumns, c => c.Name == "ExpiresAt" && c.Type == "TEXT" && c.NotNull == 0);
    }

    [Fact]
    public async Task Upgrade_ProducesSameBookingsSchema_AsFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeOperatorMcpFoundation);
        await migrator.MigrateAsync();

        Assert.Equal(GetTableSchema(freshConnection, "Bookings"), GetTableSchema(_connection, "Bookings"));
    }

    [Fact]
    public async Task FreshInstall_OperatorActionAudits_RetainRows_WhenOperatorOrRestaurantDeleted()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        await SeedAuditGraphAsync(db);

        var audit = await db.OperatorActionAudits.SingleAsync();
        var principal = await db.OperatorPrincipals.SingleAsync();
        var restaurant = await db.Restaurants.SingleAsync();

        db.OperatorPrincipals.Remove(principal);
        db.Restaurants.Remove(restaurant);
        await db.SaveChangesAsync();

        var retained = await db.OperatorActionAudits.SingleAsync();
        Assert.Equal(audit.Id, retained.Id);
        Assert.Null(retained.OperatorPrincipalId);
        Assert.Null(retained.RestaurantId);
    }

    [Fact]
    public async Task Upgrade_PreservesOperatorActionAuditDeleteSemantics_AsFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeOperatorMcpFoundation);
        await migrator.MigrateAsync();

        Assert.Equal(GetForeignKeyDeleteBehavior(freshConnection, "OperatorActionAudits", "OperatorPrincipals"), GetForeignKeyDeleteBehavior(_connection, "OperatorActionAudits", "OperatorPrincipals"));
        Assert.Equal(GetForeignKeyDeleteBehavior(freshConnection, "OperatorActionAudits", "Restaurants"), GetForeignKeyDeleteBehavior(_connection, "OperatorActionAudits", "Restaurants"));
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

    private static string GetForeignKeyDeleteBehavior(SqliteConnection connection, string tableName, string principalTable)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA foreign_key_list({tableName});";
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            if (string.Equals(reader.GetString(2), principalTable, StringComparison.Ordinal))
            {
                return reader.GetString(6);
            }
        }

        throw new InvalidOperationException($"Foreign key from {tableName} to {principalTable} not found.");
    }

    private static async Task SeedAuditGraphAsync(AppDbContext db)
    {
        var restaurant = new OpenRestoApi.Core.Domain.Restaurant
        {
            Name = "Audit Restaurant",
            OpenTime = "11:00",
            CloseTime = "13:00",
            Timezone = "UTC"
        };
        var principal = new OpenRestoApi.Core.Domain.OperatorPrincipal
        {
            Identifier = "operator@test.com",
            NormalizedIdentifier = "operator@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Restaurants.Add(restaurant);
        db.OperatorPrincipals.Add(principal);
        await db.SaveChangesAsync();

        db.OperatorActionAudits.Add(new OpenRestoApi.Core.Domain.OperatorActionAudit
        {
            OperatorPrincipalId = principal.Id,
            RestaurantId = restaurant.Id,
            Action = "reservation.list",
            Outcome = "success",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
