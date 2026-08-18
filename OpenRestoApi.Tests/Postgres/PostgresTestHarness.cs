using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Tests.Integration;

namespace OpenRestoApi.Tests.Postgres;

public sealed class PostgresTestHarness
{
    public bool IsEnabled => string.Equals(
        Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"),
        "1",
        StringComparison.Ordinal);

    public bool IsOperationalEnabled => string.Equals(
        Environment.GetEnvironmentVariable("RUN_POSTGRES_OPERATIONAL_TESTS"),
        "1",
        StringComparison.Ordinal);

    public async Task<PostgresTestDatabase> CreateDatabaseAsync(string namePrefix, bool seedApplicationData)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("RUN_POSTGRES_INTEGRATION_TESTS is not enabled.");
        }

        string? bootstrapConnectionString = Environment.GetEnvironmentVariable("POSTGRES_TEST_BOOTSTRAP_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(bootstrapConnectionString))
        {
            throw new InvalidOperationException("POSTGRES_TEST_BOOTSTRAP_CONNECTION_STRING is required when PostgreSQL integration tests are enabled.");
        }

        var bootstrapBuilder = new NpgsqlConnectionStringBuilder(bootstrapConnectionString);
        string databaseName = $"{Normalize(namePrefix)}_{Guid.NewGuid():N}".ToLowerInvariant();
        string runtimeUser = $"{databaseName}_runtime";
        string runtimePassword = $"runtime-{Guid.NewGuid():N}!";
        string backupUser = $"{databaseName}_backup";
        string backupPassword = $"backup-{Guid.NewGuid():N}!";

        await using var maintenanceConnection = new NpgsqlConnection(bootstrapConnectionString);
        await maintenanceConnection.OpenAsync();

        await using (var createDatabase = maintenanceConnection.CreateCommand())
        {
            createDatabase.CommandText = $"CREATE DATABASE \"{databaseName}\" TEMPLATE template0 ENCODING 'UTF8';";
            await createDatabase.ExecuteNonQueryAsync();
        }

        var databaseBuilder = new NpgsqlConnectionStringBuilder(bootstrapConnectionString)
        {
            Database = databaseName,
        };

        await using (var databaseConnection = new NpgsqlConnection(databaseBuilder.ConnectionString))
        {
            await databaseConnection.OpenAsync();
            await ExecuteNonQueryAsync(
                databaseConnection,
                $"""
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;
                CREATE ROLE "{runtimeUser}" LOGIN PASSWORD '{runtimePassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
                CREATE ROLE "{backupUser}" LOGIN PASSWORD '{backupPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
                GRANT CONNECT, TEMPORARY ON DATABASE "{databaseName}" TO "{runtimeUser}";
                GRANT CONNECT ON DATABASE "{databaseName}" TO "{backupUser}";
                GRANT USAGE ON SCHEMA public TO "{runtimeUser}", "{backupUser}";
                """);
        }

        await using (var db = CreatePostgresContext(databaseBuilder.ConnectionString))
        {
            await db.Database.MigrateAsync();

            if (seedApplicationData)
            {
                DbSeeder.Seed(db);
                (string hash, string salt) = new PasswordService().Hash(TestWebAppFactory.AdminPassword);
                db.AdminCredentials.Add(new AdminCredential
                {
                    Email = TestWebAppFactory.AdminEmail,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = AdminRole.SuperAdmin,
                    IsActive = true,
                });
                await db.SaveChangesAsync();
            }
        }

        await using (var databaseConnection = new NpgsqlConnection(databaseBuilder.ConnectionString))
        {
            await databaseConnection.OpenAsync();
            await ExecuteNonQueryAsync(
                databaseConnection,
                $"""
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "{runtimeUser}";
                GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO "{runtimeUser}";
                GRANT SELECT ON ALL TABLES IN SCHEMA public TO "{backupUser}";
                GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO "{backupUser}";
                ALTER DEFAULT PRIVILEGES FOR USER "{bootstrapBuilder.Username}" IN SCHEMA public
                  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "{runtimeUser}";
                ALTER DEFAULT PRIVILEGES FOR USER "{bootstrapBuilder.Username}" IN SCHEMA public
                  GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO "{runtimeUser}";
                ALTER DEFAULT PRIVILEGES FOR USER "{bootstrapBuilder.Username}" IN SCHEMA public
                  GRANT SELECT ON TABLES TO "{backupUser}";
                ALTER DEFAULT PRIVILEGES FOR USER "{bootstrapBuilder.Username}" IN SCHEMA public
                  GRANT SELECT ON SEQUENCES TO "{backupUser}";
                """);
        }

        return new PostgresTestDatabase(
            bootstrapConnectionString,
            databaseName,
            runtimeUser,
            runtimePassword,
            backupUser,
            backupPassword);
    }

    private static AppDbContext CreatePostgresContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.MigrationsAssembly("OpenRestoApi.PostgresMigrations"))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string Normalize(string value)
    {
        char[] normalized = value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        string result = new string(normalized).Trim('_');
        return result.Length <= 20 ? result : result[..20];
    }
}

public sealed class PostgresTestDatabase(
    string bootstrapConnectionString,
    string databaseName,
    string runtimeUser,
    string runtimePassword,
    string backupUser,
    string backupPassword) : IAsyncDisposable
{
    public string DatabaseName { get; } = databaseName;
    public string RuntimeUser { get; } = runtimeUser;
    public string RuntimePassword { get; } = runtimePassword;
    public string BackupUser { get; } = backupUser;
    public string BackupPassword { get; } = backupPassword;

    public string BootstrapConnectionString =>
        new NpgsqlConnectionStringBuilder(bootstrapConnectionString) { Database = databaseName }.ConnectionString;

    public string RuntimeConnectionString =>
        new NpgsqlConnectionStringBuilder(bootstrapConnectionString)
        {
            Database = databaseName,
            Username = runtimeUser,
            Password = runtimePassword,
        }.ConnectionString;

    public string BackupConnectionString =>
        new NpgsqlConnectionStringBuilder(bootstrapConnectionString)
        {
            Database = databaseName,
            Username = backupUser,
            Password = backupPassword,
        }.ConnectionString;

    public async ValueTask DisposeAsync()
    {
        var maintenanceBuilder = new NpgsqlConnectionStringBuilder(bootstrapConnectionString);

        await using var maintenanceConnection = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
        await maintenanceConnection.OpenAsync();

        await using (var terminate = maintenanceConnection.CreateCommand())
        {
            terminate.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName AND pid <> pg_backend_pid();
                """;
            terminate.Parameters.AddWithValue("databaseName", databaseName);
            await terminate.ExecuteNonQueryAsync();
        }

        await using (var dropDatabase = maintenanceConnection.CreateCommand())
        {
            dropDatabase.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
            await dropDatabase.ExecuteNonQueryAsync();
        }

        await using (var dropRoles = maintenanceConnection.CreateCommand())
        {
            dropRoles.CommandText =
                $"""
                DROP ROLE IF EXISTS "{backupUser}";
                DROP ROLE IF EXISTS "{runtimeUser}";
                """;
            await dropRoles.ExecuteNonQueryAsync();
        }
    }
}
