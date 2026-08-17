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

Set `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD` only in the protected deployment environment. Use a distinct least-privilege application role after bootstrap where operational policy requires it. Do not put passwords, `PGPASSWORD`, restic repository credentials, or alert-hook URLs in Compose files, scripts, Git, logs, or shell history.

> `Ssl Mode=Disable` in the internal overlay is appropriate only for the isolated Docker network. If the database moves off-host or onto a network not controlled by the deployment, require TLS and certificate verification instead.

## PostgreSQL backup

`scripts/postgres-backup.sh` creates a transactional `pg_dump --format=custom` archive, validates it using `pg_restore --list --verbose`, and writes a SHA-256 checksum plus a human-inspectable archive listing. It never copies PostgreSQL data files directly.

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

The restore script rejects missing/invalid checksums, a running backend, an unconfirmed database name, and restores with `--clean --if-exists --no-owner --no-privileges` only after `--apply`. Start the backend only after reviewing logs, health, expected record counts, and application smoke tests.

Run a restore drill at least quarterly and after any PostgreSQL major-version or backup-script change. It uses an isolated disposable PostgreSQL 16 container with no Compose network or host port, restores the archive, and requires at least one public table:

```bash
scripts/postgres-restore-drill.sh --archive /secure/backups/openresto-postgres-TIMESTAMP.dump
```

A successful backup is not a successful recovery plan until this drill has passed and its date/result is recorded.

## Safe EF schema migrations versus data migrations

**Schema migrations** are versioned EF Core migrations. Keep them additive and backward-compatible where possible: add nullable columns/tables/indexes first, deploy code that can tolerate both shapes, backfill separately, then enforce non-null/drop old fields only in a later release. Test a fresh database and an upgrade from the immediately preceding production migration; the existing SQLite migration-check workflow enforces this invariant for the current provider. Back up and run the restore drill before every production migration. The current backend auto-runs `Database.Migrate()` at startup, so an unsafe migration can block the application before health checks pass.

**Data migrations** are explicit, idempotent, observable application/job steps—not hidden side effects in an EF `Up()` method. Give each a version/checkpoint, bounded batches, retries, metrics/logging, validation queries, and a rollback/forward-fix plan. Do not combine an irreversible large data rewrite with a schema drop in one deployment. Run data migrations in staging on production-shaped data first, and retain a pre-change backup until business validation is complete.

## SQLite-to-PostgreSQL cutover (planned, not enabled by this overlay)

1. **Implement and test provider support first.** Add Npgsql/EF PostgreSQL support, provider-compatible migrations, and integration tests in a separate reviewed change. Do not point the current SQLite-only binary at PostgreSQL.
2. **Rehearse in isolated staging.** Build a fresh PostgreSQL schema, import a sanitized SQLite production snapshot with a repeatable conversion tool, validate counts/foreign keys/UTC timestamps/admin access/bookings, and exercise rollback.
3. **Prepare production safely.** Validate Compose, create a PostgreSQL backup/restore drill baseline, back up the SQLite database and media/DP keys, test first against the exact release candidate, and announce a write freeze.
4. **Cut over during maintenance.** Stop writes/backend, take one final consistent SQLite backup, import into PostgreSQL using the rehearsed tool, validate business totals and critical flows, then deploy the PostgreSQL-capable release with the overlay. Do not run both databases as writable sources of truth.
5. **Rollback is release + data rollback.** If validation fails before writes resume, stop the new backend and restore/restart the known-good SQLite deployment from the final backup. Once PostgreSQL accepts new writes, rollback requires an explicitly rehearsed reverse migration or a decision to repair forward; never assume `pg_dump` can reconstruct SQLite automatically.
6. **Observe before decommissioning.** Monitor errors, migration logs, connection saturation, backups, and restore-drill status. Keep the SQLite backup read-only and retained according to recovery policy; only retire it after the agreed validation window.

## Current SQLite backup and restore (until cutover)

All current persistent data lives in Docker volumes: `/data/openresto.db` in `db_data`, uploaded media in `media_data`, and Data Protection keys under `/data/dp-keys`. Before copying SQLite, checkpoint WAL or stop the backend:

```bash
docker compose exec backend sqlite3 /data/openresto.db 'PRAGMA wal_checkpoint(TRUNCATE);'
docker run --rm -v db_data:/data:ro -v "$(pwd)/backups":/backups alpine \
  tar czf /backups/openresto-db-$(date -u +%Y%m%dT%H%M%SZ).tar.gz -C /data .
```

To restore, stop the backend, restore into the correct named volume, then start it and run `PRAGMA integrity_check;`. Back up `media_data` separately when uploaded images matter. The PostgreSQL scripts do not back up SQLite volumes and must not be substituted for this procedure before the provider cutover.
