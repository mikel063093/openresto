using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public sealed class AdminCredentialManagementAuditMigrationTests : IDisposable
{
    private const string LastMigrationBeforeAdminCredentialManagementAudit = "20260727062349_AddOperatorAuditRetentionAndEscalationDurability";

    private readonly SqliteConnection _connection;

    public AdminCredentialManagementAuditMigrationTests()
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
    public async Task FreshInstall_CreatesAdminCredentialManagementAuditsTable()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        Assert.Equal("table", await GetObjectTypeAsync("AdminCredentialManagementAudits"));

        List<(string Name, string Type, long NotNull)> columns = await GetTableInfoAsync("AdminCredentialManagementAudits");
        Assert.Contains(columns, c => c.Name == "ActorEmailSnapshot" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(columns, c => c.Name == "TargetOperatorIdentifierSnapshot" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(columns, c => c.Name == "ScopeRestaurantIdsSnapshot" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(columns, c => c.Name == "TtlHoursSnapshot" && c.Type == "INTEGER" && c.NotNull == 0);
        Assert.Contains(columns, c => c.Name == "CreatedAtUtc" && c.Type == "TEXT" && c.NotNull == 1);
    }

    [Fact]
    public async Task Upgrade_CreatesSameAdminCredentialManagementAuditsSchema_AsFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeAdminCredentialManagementAudit);
        await migrator.MigrateAsync();

        Assert.Equal(
            GetTableSchema(freshConnection, "AdminCredentialManagementAudits"),
            GetTableSchema(_connection, "AdminCredentialManagementAudits"));
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
