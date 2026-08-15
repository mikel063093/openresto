using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public sealed class WhatsAppAuthorityBaselineMigrationTests : IDisposable
{
    private const string LastMigrationBeforeWhatsAppAuthorityBaseline = "20260728215409_HardenWhatsAppChannelSecurityAndIdempotency";

    private readonly SqliteConnection _connection;

    public WhatsAppAuthorityBaselineMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FreshInstall_CreatesWhatsAppSettingsAndHandoffAuditSchema()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        var restaurantColumns = await GetTableInfoAsync("Restaurants");
        var handoffColumns = await GetTableInfoAsync("WhatsAppHandoffAudits");

        Assert.Contains(restaurantColumns, c => c.Name == "IsWhatsAppTestEnabled" && c.Type == "INTEGER" && c.NotNull == 1);
        Assert.Contains(restaurantColumns, c => c.Name == "HandoffWhatsAppE164" && c.Type == "TEXT" && c.NotNull == 0);

        Assert.Equal("table", await GetObjectTypeAsync("WhatsAppHandoffAudits"));
        Assert.Contains(handoffColumns, c => c.Name == "RestaurantId" && c.Type == "INTEGER" && c.NotNull == 1);
        Assert.Contains(handoffColumns, c => c.Name == "VerifiedPhoneE164" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(handoffColumns, c => c.Name == "VerifiedPhoneNormalized" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(handoffColumns, c => c.Name == "SummarySnapshot" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(handoffColumns, c => c.Name == "HandoffDestinationSnapshot" && c.Type == "TEXT" && c.NotNull == 1);
    }

    [Fact]
    public async Task UpgradeSchema_MatchesFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeWhatsAppAuthorityBaseline);
        await migrator.MigrateAsync();

        Assert.Equal(GetTableSchema(freshConnection, "Restaurants"), GetTableSchema(_connection, "Restaurants"));
        Assert.Equal(GetTableSchema(freshConnection, "WhatsAppHandoffAudits"), GetTableSchema(_connection, "WhatsAppHandoffAudits"));
    }

    private AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(opts);
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
