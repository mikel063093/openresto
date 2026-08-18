# Backup, Restore, and PostgreSQL Operations

## Scope and current database state

The release Compose base defaults to **SQLite** (`DATABASE_PROVIDER=sqlite`). `docker-compose.postgres.yml` supplies the production PostgreSQL 16 topology and explicitly selects `DATABASE_PROVIDER=postgres`; it must be paired with a reviewed PostgreSQL-capable application image (Npgsql EF Core provider plus provider-compatible migrations). Do not deploy the overlay against an older SQLite-only binary: a PostgreSQL connection string alone cannot switch an EF Core provider.

Until that provider release is available, retain the SQLite volume backup procedure below. PostgreSQL procedures become the production runbook only after the provider cutover acceptance criteria are complete.

## PostgreSQL topology

Use the overlay with the release Compose file:

```bash
# Validate rendered configuration; values belong in an ignored deployment .env
# or protected host/service environment, never in Git.
docker compose -f docker-compose.release.yml -f docker-compose.postgres.yml config -q
```

The overlay defines `postgres:16-alpine`, a persistent `postgres_data` volume, SCRAM authentication, a health check, and a `postgres-internal` network with `internal: true`. PostgreSQL has **no host `ports` mapping**; only `backend` joins that database network. The application connection endpoint is `postgres:5432` (Docker DNS), never `localhost` or a published host port.

Set `POSTGRES_DB`, `POSTGRES_BOOTSTRAP_USER`, `POSTGRES_BOOTSTRAP_PASSWORD`, `POSTGRES_RUNTIME_USER`, `POSTGRES_RUNTIME_PASSWORD`, `POSTGRES_BACKUP_USER`, and `POSTGRES_BACKUP_PASSWORD` only in the protected deployment environment. The PostgreSQL overlay creates the runtime and backup roles during first-cluster initialization and keeps the app on the runtime role with `DATABASE_APPLY_MIGRATIONS_ON_STARTUP=false`; schema creation and data import stay explicit bootstrap actions. Do not put passwords, `PGPASSWORD`, restic repository credentials, or alert-hook URLs in Compose files, scripts, Git, logs, or shell history.

> `Ssl Mode=Disable` in the internal overlay is appropriate only for the isolated Docker network. If the database moves off-host or onto a network not controlled by the deployment, require TLS and certificate verification instead.

## PostgreSQL backup

`scripts/postgres-backup.sh` creates a transactional `pg_dump --format=custom` archive, validates it using `pg_restore --list --verbose`, and writes a SHA-256 checksum plus a human-inspectable archive listing. It authenticates with the distinct backup role over SCRAM (`PGPASSWORD` + TCP loopback inside the container) and never copies PostgreSQL data files directly.

```bash
# Run from the deployment host; Compose receives database variables from its
# protected environment file or service manager environment.
scripts/postgres-backup.sh

# A scheduler can choose a non-repository destination.
BACKUP_DIR=/var/backups/openresto/postgres scripts/postgres-backup.sh
```

Artifacts are `openresto-postgres-<UTC timestamp>.dump`, `.dump.list`, and `.dump.sha256`. Keep all three together. Local retention defaults to 14 days and is configurable with `LOCAL_BACKUP_RETENTION_DAYS`.

### Optional encrypted off-host copies: restic

Restic is opt-in. The script enables it only when `RESTIC_REPOSITORY` or `RESTIC_PASSWORD_COMMAND` is present, then requires **both**. Supply all repository/backend configuration exclusively through the scheduler/service environment (for example the appropriate S3, SFTP, or REST backend variables); never source credentials from this repository.

```bash
# Illustrative variable names only; set real values in the secret manager/service.
export RESTIC_REPOSITORY='...'
export RESTIC_PASSWORD_COMMAND='...'
export RESTIC_KEEP_DAILY=7
export RESTIC_KEEP_WEEKLY=4
export RESTIC_KEEP_MONTHLY=12
scripts/postgres-backup.sh
```

After an off-host backup, the script runs `restic forget --prune` with daily/weekly/monthly retention defaults of 7/4/12. Configure `BACKUP_ALERT_HOOK` in that same protected environment to receive a failure-only JSON POST. Alert delivery is best effort and never masks the backup failure. Schedule a daily backup and alert on both a nonzero scheduler exit and the hook; test the hook before relying on it.

Example cron entry (use a locked-down service environment rather than embedding secrets in `/etc/cron.d`):

```cron
0 03 * * * openresto /usr/bin/flock -n /var/lock/openresto-postgres-backup.lock /opt/openresto/scripts/postgres-backup.sh >>/var/log/openresto/postgres-backup.log 2>&1
```

## PostgreSQL restore and restore drills

A restore is destructive. First choose a maintenance window, stop the backend, preserve the failed volume/snapshot, verify the archive checksum, and get an independent confirmation of the exact target database.

```bash
# Non-destructive integrity and contents check.
scripts/postgres-restore.sh --archive /secure/backups/openresto-postgres-TIMESTAMP.dump --list

# Destructive restore: backend must be stopped; the exact DB name is an explicit guard.
docker compose -f docker-compose.release.yml -f docker-compose.postgres.yml stop backend
export POSTGRES_DB='the-target-name' # supplied by the protected deployment environment in normal use
scripts/postgres-restore.sh \
  --archive /secure/backups/openresto-postgres-TIMESTAMP.dump \
  --apply --confirm-database "$POSTGRES_DB"
```

The restore script rejects missing/invalid checksums, a running backend, an unconfirmed database name, and restores with `--clean --if-exists --no-owner --no-privileges` only after `--apply`. It authenticates over SCRAM using the bootstrap credential inside the container. Start the backend only after reviewing logs, health, expected record counts, and application smoke tests.

Run a restore drill at least quarterly and after any PostgreSQL major-version or backup-script change. It uses an isolated disposable PostgreSQL 16 container with SCRAM enabled, restores the archive with explicit credentials over TCP loopback, and requires at least one public table:

```bash
scripts/postgres-restore-drill.sh --archive /secure/backups/openresto-postgres-TIMESTAMP.dump
```

A successful backup is not a successful recovery plan until this drill has passed and its date/result is recorded.

## Safe EF schema migrations versus data migrations

**Schema migrations** are versioned EF Core migrations. Keep them additive and backward-compatible where possible: add nullable columns/tables/indexes first, deploy code that can tolerate both shapes, backfill separately, then enforce non-null/drop old fields only in a later release. Test a fresh database and an upgrade from the immediately preceding production migration; the existing SQLite migration-check workflow enforces this invariant for the current provider. Back up and run the restore drill before every production migration. The current backend auto-runs `Database.Migrate()` at startup, so an unsafe migration can block the application before health checks pass.

**Data migrations** are explicit, idempotent, observable application/job steps—not hidden side effects in an EF `Up()` method. Give each a version/checkpoint, bounded batches, retries, metrics/logging, validation queries, and a rollback/forward-fix plan. Do not combine an irreversible large data rewrite with a schema drop in one deployment. Run data migrations in staging on production-shaped data first, and retain a pre-change backup until business validation is complete.

## SQLite-to-PostgreSQL conversion and test cutover

The repository now includes an explicit converter project:

```bash
docker compose -f docker-compose.release.yml -f docker-compose.postgres.yml run --rm --no-deps backend \
  dotnet /app/tools/postgres-migration-tool/OpenRestoApi.PostgresMigrationTool.dll \
  --source-sqlite /data/openresto.db \
  --destination-postgres "Host=postgres;Port=5432;Database=openresto;Username=<bootstrap-user>;Password=<bootstrap-password>;Ssl Mode=Disable" \
  --report-dir /tmp/openresto-postgres-migration-reports \
  --confirm-import sqlite-to-postgres
```

The converter is intentionally fail-closed:

- source must be SQLite and readable in read-only mode
- destination must be PostgreSQL
- destination must be clean before import
- schema is built from `OpenRestoApi.PostgresMigrations`
- import order is FK-safe and deterministic
- primary keys, foreign keys, UTC timestamps, nullable fields, and enum-backed values are preserved
- PostgreSQL sequences are reseeded after import
- a redacted JSON report is written outside the repo

Use the dedicated test-only runbook for rehearsal and rollback:

- [`docs/postgres-test-cutover-runbook.md`](docs/postgres-test-cutover-runbook.md)

Production cutover remains out of scope until that test-only runbook passes with a complete evidence package and a later feature explicitly authorizes promotion.

## Current SQLite backup and restore (until cutover)

All current persistent data lives in Docker volumes: `/data/openresto.db` in `db_data`, uploaded media in `media_data`, and Data Protection keys under `/data/dp-keys`. Before copying SQLite, checkpoint WAL or stop the backend:

```bash
docker compose exec backend sqlite3 /data/openresto.db 'PRAGMA wal_checkpoint(TRUNCATE);'
docker run --rm -v db_data:/data:ro -v "$(pwd)/backups":/backups alpine \
  tar czf /backups/openresto-db-$(date -u +%Y%m%dT%H%M%SZ).tar.gz -C /data .
```

To restore, stop the backend, restore into the correct named volume, then start it and run `PRAGMA integrity_check;`. Back up `media_data` separately when uploaded images matter. The PostgreSQL scripts do not back up SQLite volumes and must not be substituted for this procedure before the provider cutover.
