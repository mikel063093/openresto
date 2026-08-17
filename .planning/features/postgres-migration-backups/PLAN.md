# PostgreSQL Migration and Verified Backups — Level-C Implementation Plan

> **For Hermes:** Execute in the isolated `feat/postgres-migration-backups` worktree. Test environment first. Never mutate the production SQLite volume until the test cutover, restore drill, production preflight backup, and rollback runbook have passed.

**Goal:** Move OpenResto persistence from SQLite to a dedicated PostgreSQL 16 service while preserving EF Core migrations and adding automated, encrypted off-host backups with restore verification.

**Verified baseline (2026-08-16):** The active test and production backend containers use `CONNECTION_STRING=Data Source=/data/openresto.db`; SQLite volumes are named `test-rest_test_rest_db_data` and `mike-openresto_openresto_db_data`. Test has 2 restaurants and zero bookings; production has 2 restaurants, 4 bookings, 1 admin credential, 17 recorded migrations, and `PRAGMA integrity_check` returned `ok`.

**Architecture:** The backend selects a relational provider from a connection-string/provider setting, with PostgreSQL the production/test target. Normal application releases still execute EF `Database.Migrate()` at startup, but SQLite-to-PostgreSQL data conversion is an explicit one-shot migration tool: source is read-only, destination is created from EF migrations, then imported transactionally with identity reseeding and table-by-table invariant checks. Backup runs in an isolated container/job using a restricted Postgres login and an encrypted remote destination configured only by injected secrets.

**Security/availability invariants:**
- No application process uses the Postgres superuser.
- Postgres is internal-only; no public `5432` port.
- No secret, dump, or decrypted archive enters Git, logs, or an image layer.
- Each mutating database migration has a tested fresh schema, upgrade path, and backup-before-deploy procedure.
- Conversion is idempotently resumable only on a clean, named destination database; it never overwrites an existing production database.
- Production SQLite and data-protection keys remain intact until post-cutover validation plus a successful restore drill.
- Automated backup success is insufficient without integrity/listing verification and an alert on failure.

## Phases

### Phase 1 — Provider-neutral application startup and tests

1. Add `Npgsql.EntityFrameworkCore.PostgreSQL` at the EF Core 10-compatible version.
2. Refactor `DatabaseExtensions` into provider-neutral connection parsing/configuration; retain SQLite startup checks only when SQLite is selected and add PostgreSQL connectivity/migration diagnostics without logging credentials.
3. Replace SQLite-only exception filters/retry strategy with provider-aware execution behavior.
4. Isolate provider-specific legacy migration-history remapping and WAL/integrity operations so they cannot execute against PostgreSQL.
5. Extend integration-test factory/configuration to use a disposable PostgreSQL instance where PostgreSQL behavior must be proved. Preserve existing SQLite tests only where they exercise legacy import.
6. Add unit/integration regressions for provider selection, redacted connection logs, Postgres startup migration, and PostgreSQL-safe initialization.

**Gate:** Backend targeted provider/startup tests and full backend test suite pass. A fresh PostgreSQL database reaches the same EF schema as the target migrations.

### Phase 2 — Explicit SQLite-to-PostgreSQL conversion tool

1. Add a CLI project or explicit `--migrate-sqlite-to-postgres` command that requires separate source and destination connection strings.
2. Reject equal endpoints, non-SQLite source, non-Postgres destination, missing confirmation flag, dirty/non-empty destination, or unknown source migration state.
3. Run destination `Database.Migrate()` first; use a single destination transaction for data import.
4. Copy tables in dependency-safe order, preserving explicit primary keys, UTC values, enum values, nullable values, and Data Protection keys stored alongside SQLite as a separately copied artifact.
5. Reseed PostgreSQL sequences using `setval` after import.
6. Compare row counts, primary-key checksums/counts, key natural identifiers, and latest EF migration on source/destination. Write a redacted JSON report outside Git.
7. Add an integration fixture that creates a representative SQLite database, converts it, and verifies it through the PostgreSQL provider, including bookings, tables, sections, operator/channel/audit entities and constraints.

**Gate:** Fresh conversion test passes; rerun refuses a non-empty destination; inserted records after conversion receive IDs above imported maxima; source file hash remains unchanged.

### Phase 3 — Compose topology and migration-aware release operations

1. Add a Postgres 16 service for test and production compose definitions, with named volume, healthcheck, non-superuser app role, internal network only, and credentials loaded from ignored environment files/secrets.
2. Switch test compose to Postgres only after the conversion tool is validated. Keep a separately named legacy SQLite volume mounted read-only only during test conversion/cutover.
3. Ensure backend waits for Postgres health and runs normal EF migrations only after it is healthy.
4. Add a one-shot migration job/service which runs manually with an explicit profile and confirmation environment variable. It must not be a normal startup container.
5. Add a production runbook: preflight, immutable SQLite snapshot, stop writes, convert, validation, healthcheck, rollback-to-SQLite procedure, and a final checkpoint for removing old volume only after retention expiry.

**Gate:** `docker compose config` validates; test stack reaches health with Postgres; legacy source remains untouched; a rollback stack can start from the retained SQLite volume.

### Phase 4 — Automated backup, off-host copy, alerts, restore drill

1. Add `scripts/backup-postgres.sh` plus a restricted backup role with `pg_read_all_data`/connect capability appropriate to the server version, but no schema/drop/create privileges.
2. Create compressed custom-format dumps via `pg_dump --format=custom`, verify them with `pg_restore --list`, calculate a checksum, and use staging/atomic rename.
3. Encrypt and upload only through injected configuration (`BACKUP_DESTINATION`, `RESTIC_REPOSITORY`, credentials/key or S3/R2-compatible variables); never commit a destination or credential. Keep local 7 daily/4 weekly/12 monthly and remote retention configured by environment.
4. Create a systemd service/timer or Docker scheduled job that runs daily, emits structured result/age metrics, and calls an injected alert webhook on failure or stale backup. Do not use an unauthenticated public endpoint.
5. Add `scripts/restore-postgres-verify.sh`: restore a selected dump into a new disposable database, verify migrations/counts/integrity queries, then destroy that verification database.
6. Update `docs/backup-restore.md` and root `CLAUDE.md` with the provider selection, schema/data-change migration rules, safe backup/restore, test-first cutover, and rollback commands.

**Gate:** local backup list and checksum verification pass; restore drill produces a valid disposable DB; timer/job configuration validates; alerts are tested with a non-secret dry-run endpoint/command.

### Phase 5 — Test then production rollout

1. Deploy only the feature branch/image to `test-rest`; take a snapshot, run the explicit migration job, validate public health/admin login/booking workflow and restore drill.
2. Record immutable evidence: image digest, source snapshot checksum, destination row checks, health response, test results, and backup artifact ID. Do not promote with a failing or missing item.
3. For production, repeat the exact preflight using its own snapshot. Schedule a maintenance window; stop backend writes, convert, activate Postgres backend, validate transactions and restore, retain SQLite rollback volume for at least the agreed retention period.
4. Configure the production automated backup job only after the Postgres cutover, validate first successful backup and restore, then enable stale/failure alerting.

## Mandatory documentation addition

The root `CLAUDE.md` must have a visible **Database connection, schema and data migrations** section near architecture/operations. It must say:
- PostgreSQL is the authoritative runtime database; use `DATABASE_PROVIDER=postgres` / `CONNECTION_STRING`, never hard-code a provider.
- EF schema migrations: update entity/configuration, generate provider-compatible migration, prove fresh + upgrade schema, back up first, deploy, check `__EFMigrationsHistory`; never rewrite applied migrations.
- Data migrations: are versioned/idempotent application migration steps or explicit one-shot tooling, must have forward validation and a rollback/restore plan, preserve UTC and identity values, and must not run invisibly during ordinary startup.
- SQLite-to-PostgreSQL conversion is a separately confirmed cutover, not a normal EF migration.

## Non-goals

- No production deployment without a passing test cutover.
- No public database port, no shared Dokploy Postgres, no plaintext off-host archive.
- No deletion of old SQLite data during this feature.
- No claim that PITR exists unless WAL archiving plus a tested recovery process is actually configured.
