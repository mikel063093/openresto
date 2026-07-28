using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public sealed class WhatsAppCustomerChannelMigrationTests : IDisposable
{
    private const string LastMigrationBeforeWhatsAppCustomerChannel = "20260727143636_AddAdminCredentialManagementAudit";

    private readonly SqliteConnection _connection;

    public WhatsAppCustomerChannelMigrationTests()
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
    public async Task FreshInstall_CreatesWhatsAppOwnershipColumns_AndIdempotencyTable()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        var bookingColumns = await GetTableInfoAsync("Bookings");
        var idempotencyColumns = await GetTableInfoAsync("ChannelMutationIdempotencyRecords");

        Assert.Contains(bookingColumns, c => c.Name == "CustomerPhoneE164" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(bookingColumns, c => c.Name == "CustomerPhoneNormalized" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(bookingColumns, c => c.Name == "ConcurrencyToken" && c.Type == "INTEGER" && c.NotNull == 1);

        Assert.Equal("table", await GetObjectTypeAsync("ChannelMutationIdempotencyRecords"));
        Assert.Contains(idempotencyColumns, c => c.Name == "State" && c.Type == "TEXT" && c.NotNull == 1);
        Assert.Contains(idempotencyColumns, c => c.Name == "ResultJson" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(idempotencyColumns, c => c.Name == "CompletedAtUtc" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(idempotencyColumns, c => c.Name == "ReplayKey" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(idempotencyColumns, c => c.Name == "ExpiresAtUtc" && c.Type == "TEXT" && c.NotNull == 0);
        Assert.Contains(
            GetIndexes(_connection, "ChannelMutationIdempotencyRecords"),
            index => index.Name == "IX_ChannelMutationIdempotencyRecords_Channel_MutationScope_IdempotencyKey" && index.IsUnique);
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
        await migrator.MigrateAsync(LastMigrationBeforeWhatsAppCustomerChannel);
        await migrator.MigrateAsync();

        Assert.Equal(GetTableSchema(freshConnection, "Bookings"), GetTableSchema(_connection, "Bookings"));
        Assert.Equal(
            GetUniqueIndexColumns(freshConnection, "ChannelMutationIdempotencyRecords"),
            GetUniqueIndexColumns(_connection, "ChannelMutationIdempotencyRecords"));
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

    private static IReadOnlyList<(string Name, bool IsUnique)> GetIndexes(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA index_list({tableName});";
        using var reader = cmd.ExecuteReader();

        var indexes = new List<(string Name, bool IsUnique)>();
        while (reader.Read())
        {
            indexes.Add((reader.GetString(1), reader.GetInt64(2) == 1));
        }

        return indexes;
    }

    private static string GetTableSchema(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name;";
        cmd.Parameters.AddWithValue("$name", tableName);
        return (string)(cmd.ExecuteScalar() ?? throw new InvalidOperationException($"Table {tableName} not found."));
    }

    private static string GetUniqueIndexColumns(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA index_list({tableName});";
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            if (reader.GetInt64(2) != 1)
            {
                continue;
            }

            string indexName = reader.GetString(1);
            using var indexCmd = connection.CreateCommand();
            indexCmd.CommandText = $"PRAGMA index_info({indexName});";
            using var indexReader = indexCmd.ExecuteReader();
            var columns = new List<string>();
            while (indexReader.Read())
            {
                columns.Add(indexReader.GetString(2));
            }

            if (columns.Count > 0)
            {
                return string.Join(",", columns);
            }
        }

        throw new InvalidOperationException($"No unique index found for {tableName}.");
    }
}
