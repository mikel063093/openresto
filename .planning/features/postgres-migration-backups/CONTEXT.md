# PostgreSQL Migration Repair Context

## Status
- Updated on Monday, August 17, 2026 as a planning-only GSD discuss pass.
- Scope is limited to the existing Level-C feature `postgres-migration-backups`.
- This pass resolves planning decisions only. It does not implement code, run destructive operations, push, or commit.

## Planning Inputs Read
- `.planning/PROJECT.md`
- `.planning/REQUIREMENTS.md`
- `.planning/ROADMAP.md`
- `.planning/STATE.md`
- `.planning/onboarding/SUMMARY.md`
- `.planning/features/postgres-migration-backups/PLAN.md`
- `.planning/features/postgres-migration-backups/VERIFICATION.md`
- `CLAUDE.md`
- `docs/backup-restore.md`
- `docker-compose.postgres.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/migration-check.yml`
- `OpenRestoApi/Extensions/DatabaseExtensions.cs`
- `OpenRestoApi.PostgresMigrations/`
- `scripts/postgres-backup.sh`
- `scripts/postgres-restore.sh`
- `scripts/postgres-restore-drill.sh`

## Verified Repository Baseline

### Provider and migration wiring already present
- `OpenRestoApi/Extensions/DatabaseExtensions.cs` selects the provider from `DATABASE_PROVIDER`.
- The SQLite branch contains SQLite-only migration-history remap and PRAGMA logic.
- The PostgreSQL branch configures `UseNpgsql(..., x => x.MigrationsAssembly("OpenRestoApi.PostgresMigrations"))`.
- `OpenRestoApi.PostgresMigrations/` exists as a dedicated PostgreSQL migrations assembly with a generated baseline migration.
- `OpenRestoApi/OpenRestoApi.csproj` references both SQLite and PostgreSQL EF providers.

### Runtime and operations posture already present
- `docker-compose.postgres.yml` uses `postgres:16-alpine`, an internal-only `postgres-internal` network, no public database port mapping, and `POSTGRES_INITDB_ARGS: --auth=scram-sha-256`.
- `docs/backup-restore.md` already distinguishes schema migrations from data migrations and already treats SQLite-to-PostgreSQL cutover as a rehearsed, explicit, test-first process.
- `scripts/postgres-backup.sh` creates a custom `pg_dump` archive, verifies it with `pg_restore --list --verbose`, and writes a SHA-256 checksum.
- `scripts/postgres-restore.sh` verifies checksum, requires `--apply`, requires exact database-name confirmation, refuses to run while the backend is still running, and restores with `--clean --if-exists --no-owner --no-privileges`.
- `scripts/postgres-restore-drill.sh` restores into a disposable PostgreSQL 16 container and fails if the archive does not materialize at least one `public` table.

### Current evidence gap
- `.github/workflows/ci.yml` and `.github/workflows/migration-check.yml` still center SQLite-backed validation paths.
- The repo contains provider wiring and PostgreSQL operational scripts, but it does not yet prove an end-to-end recoverable SQLite-to-PostgreSQL cutover path.

## Locked Decisions For This Repair

### 1. Conversion must be an explicit SQLite-to-PostgreSQL tool, not startup magic
- The migration path must use a dedicated one-shot converter or import command.
- The application startup path must not silently convert SQLite data into PostgreSQL.
- The converter must accept separate source and destination connection targets and must refuse same-endpoint or provider-mismatch input.
- The converter must build the PostgreSQL schema from `OpenRestoApi.PostgresMigrations` before data import.

### 2. Destination safety is fail-closed
- The converter must refuse a nonempty destination database.
- “Nonempty” means more than the expected clean schema/bootstrap state for the target PostgreSQL migrations lineage.
- Reruns against a previously imported database are not allowed unless the destination has been deliberately re-created as a clean target.

### 3. Data fidelity rules are non-negotiable
- Imported data must preserve:
  - primary keys
  - foreign-key relationships
  - UTC semantics for persisted `DateTime` values
  - enum semantics as currently stored/translated by the repo
  - nullable values
  - all persisted operational tables required by current OpenResto behavior
- Sequence/identity values must be reseeded after import so subsequent PostgreSQL writes do not collide with preserved IDs.

### 4. Operational roles are separate by design
- The implementation must define distinct PostgreSQL responsibilities for:
  - bootstrap and migration application
  - steady-state application runtime
  - logical backup execution
  - restore-drill verification
- The runtime application connection must not rely on the PostgreSQL superuser.
- Backup and restore-drill roles should avoid create/drop privileges in normal operation; only the isolated drill environment may hold broader privileges where the drill itself requires them.

### 5. SCRAM and logical backup/restore remain the recovery model
- The PostgreSQL operational path stays aligned with `POSTGRES_INITDB_ARGS: --auth=scram-sha-256`.
- Backup/restore remains logical-dump based:
  - `pg_dump --format=custom`
  - `pg_restore --list --verbose`
  - SHA-256 checksum creation and verification
- Raw PostgreSQL data-directory copying is not an acceptable backup strategy for this feature.
- Backup success is not enough; restore-drill success is a required acceptance artifact.

### 6. CI must prove PostgreSQL behavior, not just SQLite migration safety
- SQLite-only migration SQL checks are insufficient for provider-cutover sign-off.
- CI must add a PostgreSQL-aware path that proves:
  - fresh PostgreSQL schema creation from the dedicated migrations assembly
  - application startup against PostgreSQL
  - representative PostgreSQL integration coverage
  - provider-aware migration validation separate from the current SQLite schema-diff check

### 7. Cutover and rollback stay test-only in this track
- The first real exercise is a test-only cutover.
- Production adoption is explicitly out of scope for this feature.
- Rollback means preserving and proving the original SQLite snapshot and tested release path before PostgreSQL becomes the only writable source.

## Execution-Shaping Decisions

### Conversion model
- The converter should be planned as a read-only-from-source, write-only-to-destination workflow with a report artifact written outside Git.
- The converter should validate source/destination/provider preconditions before touching PostgreSQL data.
- The converter should import in deterministic dependency-safe order rather than relying on ad hoc table iteration.

### Validation artifact
- The converter must emit a redacted validation report outside Git containing:
  - source and destination provider identification
  - migration lineage used to build the destination schema
  - per-table row-count comparisons
  - key invariants and sequence maxima
  - success/failure summary for the rehearsal

### Test cutover evidence
- Test cutover evidence must include:
  - immutable pre-cutover SQLite snapshot
  - exact release candidate identity
  - converter validation report
  - backend health after PostgreSQL startup
  - admin login success
  - representative booking-flow success
  - PostgreSQL backup artifact
  - successful restore-drill record tied to that backup

### Rollback evidence
- Rollback evidence must prove:
  - the retained SQLite snapshot remains untouched and startable
  - rollback steps are written against the same test release candidate used for cutover
  - rollback is executed before PostgreSQL becomes the only writable source if acceptance checks fail

## Agent's Discretion
- The execution planner may choose the concrete host shape of the explicit converter:
  - standalone console/tooling project
  - tightly scoped backend-adjacent maintenance command
  - separate migration utility
- The planner may choose the exact integration-test harness for PostgreSQL CI if it stays inside repo-supported patterns and does not require production secrets.
- The planner may choose the exact row-ordering strategy for import so long as it preserves IDs, FKs, UTC semantics, and deterministic validation.

## Scope Fence
- No application-code changes in this discuss pass.
- No destructive database actions in this discuss pass.
- No production cutover design approval in this feature.
- No assumption that existing PostgreSQL scripts alone prove migration readiness.
- No dual-write or mixed-source-of-truth design is allowed.

## Deferred Ideas
- Managed PostgreSQL hosting, external secret managers, or off-host TLS requirements beyond the current internal Docker topology are deferred unless a later phase is explicitly scoped for them.
- Any reverse PostgreSQL-to-SQLite converter is deferred; this track only requires rollback before PostgreSQL becomes the sole writable source.
