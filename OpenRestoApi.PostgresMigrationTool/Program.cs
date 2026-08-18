using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.PostgresMigrationTool;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            MigrationCommand command = MigrationCommand.Parse(args, Directory.GetCurrentDirectory());
            MigrationReport report = await new PostgresMigrationRunner().RunAsync(command, CancellationToken.None);
            Console.WriteLine($"SQLite-to-PostgreSQL migration completed. Report: {report.ReportPath}");
            return 0;
        }
        catch (MigrationUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(MigrationCommand.Usage);
            return 2;
        }
        catch (MigrationRefusalException ex)
        {
            Console.Error.WriteLine($"Refused: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

public sealed record MigrationCommand(
    string SourceSqlitePath,
    string DestinationConnectionString,
    string ReportDirectory,
    string RepoRoot)
{
    public const string ConfirmationToken = "sqlite-to-postgres";

    public static string Usage =>
        """
        Usage:
          dotnet run --project OpenRestoApi.PostgresMigrationTool -- \
            --source-sqlite /absolute/path/openresto.db \
            --destination-postgres "Host=localhost;Port=5432;Database=openresto;Username=bootstrap;Password=***;Ssl Mode=Disable" \
            --report-dir /tmp/openresto-postgres-migration-reports \
            --confirm-import sqlite-to-postgres
        """;

    public static MigrationCommand Parse(IReadOnlyList<string> args, string repoRoot)
    {
        string? source = null;
        string? destination = null;
        string? reportDir = null;
        string? confirmation = null;

        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--source-sqlite":
                    source = GetValue(args, ref i, "--source-sqlite");
                    break;
                case "--destination-postgres":
                    destination = GetValue(args, ref i, "--destination-postgres");
                    break;
                case "--report-dir":
                    reportDir = GetValue(args, ref i, "--report-dir");
                    break;
                case "--confirm-import":
                    confirmation = GetValue(args, ref i, "--confirm-import");
                    break;
                case "-h":
                case "--help":
                    throw new MigrationUsageException("Help requested.");
                default:
                    throw new MigrationUsageException($"Unknown argument: {args[i]}");
            }
        }

        if (string.IsNullOrWhiteSpace(source) ||
            string.IsNullOrWhiteSpace(destination) ||
            string.IsNullOrWhiteSpace(reportDir))
        {
            throw new MigrationUsageException("Missing required arguments.");
        }

        if (!string.Equals(confirmation, ConfirmationToken, StringComparison.Ordinal))
        {
            throw new MigrationRefusalException(
                $"Missing exact confirmation token. Re-run with --confirm-import {ConfirmationToken}.");
        }

        return new MigrationCommand(
            Path.GetFullPath(source),
            destination,
            Path.GetFullPath(reportDir),
            Path.GetFullPath(repoRoot));
    }

    private static string GetValue(IReadOnlyList<string> args, ref int index, string name)
    {
        if (index + 1 >= args.Count)
        {
            throw new MigrationUsageException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }
}

public sealed class PostgresMigrationRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static readonly Type[] ImportOrder =
    [
        typeof(AdminCredential),
        typeof(BrandSettings),
        typeof(ChannelMutationIdempotencyRecord),
        typeof(EmailFailure),
        typeof(EmailSettings),
        typeof(RestaurantHighlight),
        typeof(OperatorPrincipal),
        typeof(Restaurant),
        typeof(SocialLink),
        typeof(Section),
        typeof(Table),
        typeof(AdminPushSubscription),
        typeof(OperatorAgentCredential),
        typeof(OperatorRestaurantScope),
        typeof(RestaurantOccasionCatalogItem),
        typeof(Booking),
        typeof(OperatorAgentCredentialScope),
        typeof(AdminCredentialManagementAudit),
        typeof(AdminNotification),
        typeof(OperatorActionAudit),
        typeof(BookingOccasionSnapshot),
        typeof(WhatsAppHandoffAudit),
    ];

    public Func<Type, CancellationToken, Task>? AfterEntityImportedAsync { get; init; }

    public async Task<MigrationReport> RunAsync(MigrationCommand command, CancellationToken cancellationToken)
    {
        EnsureExternalReportDirectory(command);
        Directory.CreateDirectory(command.ReportDirectory);

        var destinationBuilder = new NpgsqlConnectionStringBuilder(command.DestinationConnectionString);
        RefuseIfDestinationLooksLikeSource(command.SourceSqlitePath, destinationBuilder);

        await using AppDbContext source = CreateSqliteContext(command.SourceSqlitePath);
        await using AppDbContext destination = CreatePostgresContext(command.DestinationConnectionString);

        await ValidateSourceAsync(source, cancellationToken);
        await EnsureDestinationCleanAsync(destination, cancellationToken);

        await destination.Database.MigrateAsync(cancellationToken);
        await EnsureDestinationCleanAsync(destination, cancellationToken);

        var tableReports = new List<TableMigrationReport>();
        await using var transaction = await destination.Database.BeginTransactionAsync(cancellationToken);

        foreach (Type entityType in ImportOrder)
        {
            TableMigrationReport tableReport = await ImportEntityAsync(source, destination, entityType, cancellationToken);
            tableReports.Add(tableReport);
            if (AfterEntityImportedAsync is not null)
            {
                await AfterEntityImportedAsync(entityType, cancellationToken);
            }
        }

        await ReseedSequencesAsync(destination, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        IReadOnlyList<string> appliedMigrations = (await destination.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        string reportPath = await WriteReportAsync(command, destinationBuilder, appliedMigrations, tableReports, destination, cancellationToken);

        return new MigrationReport(reportPath, tableReports);
    }

    private static void EnsureExternalReportDirectory(MigrationCommand command)
    {
        string repoRoot = EnsureTrailingSeparator(command.RepoRoot);
        string reportRoot = EnsureTrailingSeparator(command.ReportDirectory);
        if (reportRoot.StartsWith(repoRoot, StringComparison.Ordinal))
        {
            throw new MigrationRefusalException("Report directory must be outside the Git worktree.");
        }
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static void RefuseIfDestinationLooksLikeSource(string sourceSqlitePath, NpgsqlConnectionStringBuilder destinationBuilder)
    {
        if (string.Equals(destinationBuilder.Database, Path.GetFileNameWithoutExtension(sourceSqlitePath), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(destinationBuilder.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new MigrationRefusalException("Source and destination must be separate endpoints; refusing suspicious same-name localhost target.");
        }
    }

    private static AppDbContext CreateSqliteContext(string sourceSqlitePath)
    {
        if (!File.Exists(sourceSqlitePath))
        {
            throw new MigrationRefusalException($"SQLite source does not exist: {sourceSqlitePath}");
        }

        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = sourceSqlitePath,
            Mode = SqliteOpenMode.ReadOnly,
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(builder.ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AppDbContext CreatePostgresContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly("OpenRestoApi.PostgresMigrations"))
            .Options;

        return new AppDbContext(options);
    }

    private static async Task ValidateSourceAsync(AppDbContext source, CancellationToken cancellationToken)
    {
        if (!await source.Database.CanConnectAsync(cancellationToken))
        {
            throw new MigrationRefusalException("SQLite source is not readable.");
        }

        IReadOnlyList<string> pending = (await source.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            throw new MigrationRefusalException(
                $"SQLite source has unsupported or indeterminate migration state. Pending migrations: {string.Join(", ", pending)}");
        }
    }

    private static async Task EnsureDestinationCleanAsync(AppDbContext destination, CancellationToken cancellationToken)
    {
        if (!await destination.Database.CanConnectAsync(cancellationToken))
        {
            throw new MigrationRefusalException("PostgreSQL destination is not reachable.");
        }

        NpgsqlConnection connection = (NpgsqlConnection)destination.Database.GetDbConnection();
        bool opened = false;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            opened = true;
        }

        try
        {
            await using var tablesCommand = new NpgsqlCommand(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                ORDER BY table_name;
                """,
                connection);
            List<string> tables = [];
            await using (var reader = await tablesCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    tables.Add(reader.GetString(0));
                }
            }

            if (tables.Count == 0)
            {
                return;
            }

            var entityTables = destination.Model.GetEntityTypes()
                .Select(entity => entity.GetTableName())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string table in tables)
            {
                if (string.Equals(table, "__EFMigrationsHistory", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!entityTables.Contains(table))
                {
                    throw new MigrationRefusalException($"Destination contains unexpected public table '{table}'.");
                }

                await using var countCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\";", connection);
                long rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
                if (rowCount > 0)
                {
                    throw new MigrationRefusalException($"Destination is not clean; table '{table}' already contains {rowCount} row(s).");
                }
            }
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<TableMigrationReport> ImportEntityAsync(
        AppDbContext source,
        AppDbContext destination,
        Type entityType,
        CancellationToken cancellationToken)
    {
        IQueryable sourceQuery = GetEntityQuery(source, entityType);
        var sourceRows = await sourceQuery.Cast<object>().ToListAsync(cancellationToken);
        List<object> clonedRows = sourceRows.Select(CloneScalarEntity).ToList();

        await destination.AddRangeAsync(clonedRows, cancellationToken);
        await destination.SaveChangesAsync(cancellationToken);
        destination.ChangeTracker.Clear();

        int? maxId = GetMaxIdentityValue(sourceRows);
        int destinationCount = await GetEntityQuery(destination, entityType).Cast<object>().CountAsync(cancellationToken);
        return new TableMigrationReport(
            entityType.Name,
            sourceRows.Count,
            destinationCount,
            maxId);
    }

    private static IQueryable GetEntityQuery(DbContext context, Type entityType)
    {
        var setMethod = typeof(DbContext).GetMethods()
            .Single(method => method.Name == nameof(DbContext.Set)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 0);

        return (IQueryable)(setMethod.MakeGenericMethod(entityType).Invoke(context, null)
            ?? throw new InvalidOperationException($"Could not create query for {entityType.FullName}"));
    }

    private static object CloneScalarEntity(object source)
    {
        Type type = source.GetType();
        object clone = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not create instance of {type.FullName}");

        foreach (var property in type.GetProperties().Where(IsScalarWritableProperty))
        {
            property.SetValue(clone, property.GetValue(source));
        }

        return clone;
    }

    private static bool IsScalarWritableProperty(System.Reflection.PropertyInfo property)
    {
        if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (propertyType.IsEnum)
        {
            return true;
        }

        if (propertyType == typeof(string) ||
            propertyType == typeof(int) ||
            propertyType == typeof(long) ||
            propertyType == typeof(bool) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(Guid))
        {
            return true;
        }

        return false;
    }

    private static int? GetMaxIdentityValue(IEnumerable<object> rows)
    {
        List<int> ids = rows
            .Select(row => row.GetType().GetProperty("Id")?.GetValue(row))
            .OfType<int>()
            .ToList();

        return ids.Count == 0 ? null : ids.Max();
    }

    private static async Task ReseedSequencesAsync(
        AppDbContext destination,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = (NpgsqlConnection)destination.Database.GetDbConnection();
        bool opened = false;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            opened = true;
        }

        try
        {
            NpgsqlTransaction dbTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
                ?? throw new InvalidOperationException("Sequence reseed requires an active relational PostgreSQL transaction.");

            foreach (var entity in destination.Model.GetEntityTypes())
            {
                string? table = entity.GetTableName();
                var key = entity.FindPrimaryKey();
                if (string.IsNullOrWhiteSpace(table) || key is null || key.Properties.Count != 1)
                {
                    continue;
                }

                var idProperty = key.Properties[0];
                if (idProperty.ClrType != typeof(int))
                {
                    continue;
                }

                string column = idProperty.GetColumnName() ?? idProperty.Name;
                string sql =
                    $"SELECT setval(pg_get_serial_sequence('\"{table}\"', '{column}'), COALESCE(MAX(\"{column}\"), 1), MAX(\"{column}\") IS NOT NULL) FROM \"{table}\";";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Transaction = dbTransaction;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<string> WriteReportAsync(
        MigrationCommand command,
        NpgsqlConnectionStringBuilder destinationBuilder,
        IReadOnlyList<string> appliedMigrations,
        IReadOnlyList<TableMigrationReport> tables,
        AppDbContext destination,
        CancellationToken cancellationToken)
    {
        var report = new
        {
            generatedAtUtc = DateTime.UtcNow,
            source = new
            {
                provider = "sqlite",
                fileName = Path.GetFileName(command.SourceSqlitePath),
                fileSha256 = Sha256(File.ReadAllBytes(command.SourceSqlitePath)),
            },
            destination = new
            {
                provider = "postgres",
                host = destinationBuilder.Host,
                port = destinationBuilder.Port,
                database = destinationBuilder.Database,
                username = destinationBuilder.Username,
                passwordRedacted = true,
            },
            postgresMigrationsAssembly = "OpenRestoApi.PostgresMigrations",
            appliedMigrations,
            tables,
            sequenceStatus = tables
                .Where(table => table.MaxIdentityValue is not null)
                .Select(table => new
                {
                    table = table.Table,
                    expectedMinimumNextValue = table.MaxIdentityValue!.Value + 1,
                })
                .ToList(),
            destinationProvider = destination.Database.ProviderName,
            success = tables.All(table => table.SourceRowCount == table.DestinationRowCount),
        };

        string reportPath = Path.Combine(
            command.ReportDirectory,
            $"openresto-postgres-migration-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json");
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, JsonOptions), cancellationToken);
        return reportPath;
    }

    private static string Sha256(byte[] bytes)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}

public sealed record MigrationReport(string ReportPath, IReadOnlyList<TableMigrationReport> Tables);

public sealed record TableMigrationReport(
    string Table,
    int SourceRowCount,
    int DestinationRowCount,
    int? MaxIdentityValue);

public sealed class MigrationUsageException(string message) : Exception(message);

public sealed class MigrationRefusalException(string message) : Exception(message);
