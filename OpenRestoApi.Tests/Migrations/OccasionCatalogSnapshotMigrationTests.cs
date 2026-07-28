using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public sealed class OccasionCatalogSnapshotMigrationTests : IDisposable
{
    private const string LastMigrationBeforeOccasionCatalog = "20260728210202_AddWhatsAppCustomerChannelFoundation";
    private const string LastMigrationBeforeOccasionSnapshotHardening = "20260728210525_AddOccasionCatalogAndBookingSnapshots";

    private readonly SqliteConnection _connection;

    public OccasionCatalogSnapshotMigrationTests()
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
    public async Task FreshInstall_CreatesCatalogAndSnapshotTables()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        Assert.Equal("table", await GetObjectTypeAsync("RestaurantOccasionCatalogItems"));
        Assert.Equal("table", await GetObjectTypeAsync("BookingOccasionSnapshots"));
        Assert.Equal(
            "CREATE UNIQUE INDEX \"IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId\" ON \"BookingOccasionSnapshots\" (\"BookingId\", \"RestaurantOccasionCatalogItemId\")",
            GetIndexSchema(_connection, "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId"));
    }

    [Fact]
    public async Task Upgrade_ProducesSameCatalogSchema_AsFreshInstall()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeOccasionCatalog);
        await migrator.MigrateAsync();

        Assert.Equal(
            GetTableSchema(freshConnection, "RestaurantOccasionCatalogItems"),
            GetTableSchema(_connection, "RestaurantOccasionCatalogItems"));
        Assert.Equal(
            GetTableSchema(freshConnection, "BookingOccasionSnapshots"),
            GetTableSchema(_connection, "BookingOccasionSnapshots"));
        Assert.Equal(
            GetIndexSchema(freshConnection, "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId"),
            GetIndexSchema(_connection, "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId"));
    }

    [Fact]
    public async Task Upgrade_FromPreHardeningMigration_AddsSnapshotUniquenessIndex()
    {
        using AppDbContext db = CreateContext();
        IMigrator migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();

        await migrator.MigrateAsync(LastMigrationBeforeOccasionSnapshotHardening);
        Assert.Null(GetIndexSchema(_connection, "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId"));

        await migrator.MigrateAsync();

        Assert.Equal(
            "CREATE UNIQUE INDEX \"IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId\" ON \"BookingOccasionSnapshots\" (\"BookingId\", \"RestaurantOccasionCatalogItemId\")",
            GetIndexSchema(_connection, "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId"));
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

    private static string GetTableSchema(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name;";
        cmd.Parameters.AddWithValue("$name", tableName);
        return (string)(cmd.ExecuteScalar() ?? throw new InvalidOperationException($"Table {tableName} not found."));
    }

    private static string? GetIndexSchema(SqliteConnection connection, string indexName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = $name;";
        cmd.Parameters.AddWithValue("$name", indexName);
        return (string?)cmd.ExecuteScalar();
    }
}
