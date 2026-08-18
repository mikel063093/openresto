using System.Diagnostics;
using System.Text;
using Npgsql;

namespace OpenRestoApi.Tests.Postgres;

[Collection(PostgresIntegrationCollection.Name)]
public sealed class PostgresOperationalIntegrationTests(PostgresTestHarness harness)
{
    private readonly PostgresTestHarness _harness = harness;

    [Fact]
    public async Task ComposeTopology_UsesScramRoles_AndBackupRestoreFlowPasses()
    {
        if (!_harness.IsOperationalEnabled)
        {
            return;
        }

        string repoRoot = GetRepoRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"openresto-postgres-ops-{Guid.NewGuid():N}");
        string fixturesDir = Path.Combine(tempRoot, "fixtures");
        string reportsDir = Path.Combine(tempRoot, "reports");
        string backupsDir = Path.Combine(tempRoot, "backups");
        Directory.CreateDirectory(fixturesDir);
        Directory.CreateDirectory(reportsDir);
        Directory.CreateDirectory(backupsDir);
        EnsureWorldWritable(fixturesDir);
        EnsureWorldWritable(reportsDir);
        EnsureWorldWritable(backupsDir);

        string sqlitePath = await PostgresMigrationTestData.CreateSqliteSourceAsync(nameof(ComposeTopology_UsesScramRoles_AndBackupRestoreFlowPasses), seedRichData: true);
        string mountedSqlitePath = Path.Combine(fixturesDir, "source.db");
        File.Copy(sqlitePath, mountedSqlitePath, overwrite: true);

        string composeProject = $"openresto-pgops-{Guid.NewGuid().ToString("N")[..12]}";
        string postgresDb = $"pgops_{Guid.NewGuid().ToString("N")[..12]}";
        string bootstrapUser = "bootstrap";
        string bootstrapPassword = $"bootstrap-{Guid.NewGuid():N}!";
        string runtimeUser = "runtime_ops";
        string runtimePassword = $"runtime-{Guid.NewGuid():N}!";
        string backupUser = "backup_ops";
        string backupPassword = $"backup-{Guid.NewGuid():N}!";

        Dictionary<string, string> environment = new(StringComparer.Ordinal)
        {
            ["COMPOSE_PROJECT_NAME"] = composeProject,
            ["POSTGRES_DB"] = postgresDb,
            ["POSTGRES_BOOTSTRAP_USER"] = bootstrapUser,
            ["POSTGRES_BOOTSTRAP_PASSWORD"] = bootstrapPassword,
            ["POSTGRES_RUNTIME_USER"] = runtimeUser,
            ["POSTGRES_RUNTIME_PASSWORD"] = runtimePassword,
            ["POSTGRES_BACKUP_USER"] = backupUser,
            ["POSTGRES_BACKUP_PASSWORD"] = backupPassword,
            ["ADMIN_EMAIL"] = "ops-admin@example.com",
            ["ADMIN_PASSWORD"] = "OpsPass123!OpsPass123!",
            ["BACKUP_DIR"] = backupsDir,
        };

        string composeArgs = "-f docker-compose.yml -f docker-compose.postgres.yml";

        try
        {
            await RunBashAsync($"docker compose {composeArgs} build backend", repoRoot, environment, TimeSpan.FromMinutes(20));
            await RunBashAsync($"docker compose {composeArgs} up -d postgres", repoRoot, environment, TimeSpan.FromMinutes(5));
            await WaitForPostgresAsync(repoRoot, environment, composeArgs, bootstrapUser, postgresDb);

            string passwordQuery =
                "SELECT count(*) FROM pg_authid WHERE rolname IN ('bootstrap','runtime_ops','backup_ops') AND rolpassword LIKE 'SCRAM-SHA-256%';";
            CommandResult scramRoles = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                bootstrapUser,
                bootstrapPassword,
                postgresDb,
                passwordQuery);
            Assert.Equal("3", scramRoles.Stdout.Trim());

            CommandResult encryptionMode = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                bootstrapUser,
                bootstrapPassword,
                postgresDb,
                "SHOW password_encryption;");
            Assert.Equal("scram-sha-256", encryptionMode.Stdout.Trim());

            string destinationConnectionString =
                $"Host=postgres;Port=5432;Database={postgresDb};Username={bootstrapUser};Password={bootstrapPassword};Ssl Mode=Disable";
            string converterCommand =
                "docker compose " + composeArgs +
                $" run --rm --no-deps --entrypoint dotnet -v \"{tempRoot}:/mnt/test-fixtures\" backend " +
                "/app/tools/postgres-migration-tool/OpenRestoApi.PostgresMigrationTool.dll " +
                "--source-sqlite /mnt/test-fixtures/fixtures/source.db " +
                $"--destination-postgres \"{destinationConnectionString}\" " +
                "--report-dir /mnt/test-fixtures/reports " +
                "--confirm-import sqlite-to-postgres";
            await RunBashAsync(converterCommand, repoRoot, environment, TimeSpan.FromMinutes(10));
            Assert.Single(Directory.GetFiles(reportsDir, "*.json"));

            CommandResult runtimeSelect = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                runtimeUser,
                runtimePassword,
                postgresDb,
                "SELECT count(*) FROM \"Restaurants\";");
            Assert.Equal("1", runtimeSelect.Stdout.Trim());

            CommandResult runtimeDdl = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                runtimeUser,
                runtimePassword,
                postgresDb,
                "CREATE TABLE runtime_role_should_fail(id integer);",
                expectSuccess: false);
            Assert.Contains("permission denied", runtimeDdl.CombinedOutput, StringComparison.OrdinalIgnoreCase);

            CommandResult backupSelect = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                backupUser,
                backupPassword,
                postgresDb,
                "SELECT count(*) FROM \"Bookings\";");
            Assert.Equal("1", backupSelect.Stdout.Trim());

            CommandResult backupInsert = await RunComposeSqlAsync(
                repoRoot,
                environment,
                composeArgs,
                backupUser,
                backupPassword,
                postgresDb,
                "INSERT INTO \"Restaurants\" (\"Name\", \"Timezone\") VALUES ('Should Fail', 'UTC');",
                expectSuccess: false);
            Assert.Contains("permission denied", backupInsert.CombinedOutput, StringComparison.OrdinalIgnoreCase);

            await RunBashAsync(
                $"scripts/postgres-backup.sh --compose-file docker-compose.yml --compose-file docker-compose.postgres.yml --backup-dir \"{backupsDir}\"",
                repoRoot,
                environment,
                TimeSpan.FromMinutes(10));

            string archive = Assert.Single(Directory.GetFiles(backupsDir, "*.dump"));
            Assert.True(File.Exists(archive + ".list"));
            Assert.True(File.Exists(archive + ".sha256"));

            await RunBashAsync(
                $"scripts/postgres-restore-drill.sh --archive \"{archive}\"",
                repoRoot,
                environment,
                TimeSpan.FromMinutes(10));
        }
        finally
        {
            await RunBashAsync(
                $"docker compose {composeArgs} down -v --remove-orphans",
                repoRoot,
                environment,
                TimeSpan.FromMinutes(5),
                expectSuccess: false);

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }

            if (File.Exists(sqlitePath))
            {
                File.Delete(sqlitePath);
            }
        }
    }

    private static async Task WaitForPostgresAsync(
        string repoRoot,
        IReadOnlyDictionary<string, string> environment,
        string composeArgs,
        string username,
        string database)
    {
        for (int attempt = 0; attempt < 60; attempt++)
        {
            CommandResult result = await RunBashAsync(
                "docker compose " + composeArgs +
                $" exec -T postgres pg_isready -h 127.0.0.1 -U \"{username}\" -d \"{database}\"",
                repoRoot,
                environment,
                TimeSpan.FromSeconds(15),
                expectSuccess: false);
            if (result.ExitCode == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new Xunit.Sdk.XunitException("PostgreSQL did not become ready inside the compose-backed operational test.");
    }

    private static Task<CommandResult> RunComposeSqlAsync(
        string repoRoot,
        IReadOnlyDictionary<string, string> environment,
        string composeArgs,
        string username,
        string password,
        string database,
        string sql,
        bool expectSuccess = true)
    {
        string escapedPassword = password.Replace("\"", "\\\"", StringComparison.Ordinal);
        string escapedSql = sql.Replace("\"", "\\\"", StringComparison.Ordinal);
        string command =
            "docker compose " + composeArgs +
            $" exec -T postgres env PGPASSWORD=\"{escapedPassword}\" " +
            $"psql -h 127.0.0.1 -U \"{username}\" -d \"{database}\" -v ON_ERROR_STOP=1 -Atqc \"{escapedSql}\"";
        return RunBashAsync(command, repoRoot, environment, TimeSpan.FromMinutes(2), expectSuccess);
    }

    private static async Task<CommandResult> RunBashAsync(
        string command,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        TimeSpan timeout,
        bool expectSuccess = true)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-lc \"{command.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach ((string key, string value) in environment)
        {
            process.StartInfo.Environment[key] = value;
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stdout.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stderr.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            throw new Xunit.Sdk.XunitException($"Command timed out after {timeout}: {command}");
        }

        var result = new CommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        if (expectSuccess && result.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Command failed with exit code {result.ExitCode}: {command}{Environment.NewLine}{result.CombinedOutput}");
        }

        return result;
    }

    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static void EnsureWorldWritable(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
    }

    private sealed record CommandResult(int ExitCode, string Stdout, string Stderr)
    {
        public string CombinedOutput => Stdout + Stderr;
    }
}
