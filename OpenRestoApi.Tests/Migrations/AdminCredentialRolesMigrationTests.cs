using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Migrations;

public class AdminCredentialRolesMigrationTests : IDisposable
{
    private const string LastMigrationBeforeRoles = "20260720102242_AddRestaurantMaxTableOversizeSeats";

    private readonly SqliteConnection _connection;

    public AdminCredentialRolesMigrationTests()
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
    public async Task FreshInstall_Creates_Role_As_NotNull_Integer_With_Default_Zero()
    {
        using AppDbContext db = CreateContext();
        await db.Database.MigrateAsync();

        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA table_info(AdminCredentials);";
        using var reader = await pragma.ExecuteReaderAsync();

        bool foundRole = false;
        while (await reader.ReadAsync())
        {
            if (reader.GetString(1) == "Role")
            {
                foundRole = true;
                Assert.Equal("INTEGER", reader.GetString(2));
                Assert.Equal(1L, reader.GetInt64(3));
                Assert.Equal("0", reader.GetString(4));
            }
        }

        Assert.True(foundRole, "AdminCredentials.Role should exist as a required INTEGER column.");
    }

    [Fact]
    public async Task Upgrade_Promotes_Existing_AdminCredential_To_SuperAdmin_Without_Data_Migration()
    {
        using AppDbContext db = CreateContext();
        IMigrator migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();

        await migrator.MigrateAsync(LastMigrationBeforeRoles);

        using (var insert = _connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO AdminCredentials (Email, PasswordHash, PasswordSalt)
                VALUES ('legacy@example.com', 'hash', 'salt');
                """;
            await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync();

        using var query = _connection.CreateCommand();
        query.CommandText = """
            SELECT Email, Role, IsActive
            FROM AdminCredentials
            WHERE Email = 'legacy@example.com';
            """;
        using var reader = await query.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("legacy@example.com", reader.GetString(0));
        Assert.Equal(0, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }

    [Fact]
    public async Task Upgrade_Produces_Same_AdminCredentials_Schema_As_Fresh_Install()
    {
        using var freshConnection = new SqliteConnection("Data Source=:memory:");
        freshConnection.Open();
        using (var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(freshConnection).Options))
        {
            await freshDb.Database.MigrateAsync();
        }

        using AppDbContext upgradeDb = CreateContext();
        IMigrator migrator = upgradeDb.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync(LastMigrationBeforeRoles);
        await migrator.MigrateAsync();

        Assert.Equal(
            GetTableSchema(freshConnection, "AdminCredentials"),
            GetTableSchema(_connection, "AdminCredentials"));
    }

    private static string GetTableSchema(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{tableName}';";
        return (string)(cmd.ExecuteScalar() ?? throw new InvalidOperationException($"{tableName} table not found."));
    }
}
