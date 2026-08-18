# PostgreSQL Migration Repair Plan

## Goal
Turn the repository's partial PostgreSQL support into a reviewable, test-first, rollback-first SQLite-to-PostgreSQL migration path that can be rehearsed safely in test before any production consideration.

## Planning Rules
- Planning artifact only. No code implementation, destructive operations, pushes, or commits are authorized from this worktree.
- Respect locked decisions in `.planning/features/postgres-migration-backups/CONTEXT.md`.
- Keep SQLite as the currently recoverable source of truth until the test-only cutover and rollback gates are proven.
- Treat PostgreSQL support as a provider migration repair, not as a normal incremental feature.
- Prefer additive, isolated work slices with explicit verification over one large migration change.

## Locked Acceptance Gates
- No implementation is acceptable if the SQLite-to-PostgreSQL conversion path is implicit, hidden in startup, or tolerant of a nonempty destination.
- No runtime deployment is acceptable if the runtime role still requires PostgreSQL superuser privileges.
- No backup sign-off is acceptable without both archive verification and restore-drill proof.
- No provider-cutover sign-off is acceptable while CI remains SQLite-only.
- No production promotion is acceptable from this feature; the highest authorized environment is test cutover rehearsal.

## Dependency Graph
- Slice 1 establishes the provider/migration baseline used by every later slice.
- Slice 2 depends on Slice 1 for the authoritative schema and runtime seams.
- Slice 3 depends on Slice 2 because operational roles must match the explicit converter and runtime shape.
- Slice 4 depends on Slices 1 through 3 because CI must validate the real provider/migration/role model.
- Slice 5 depends on Slices 2 through 4 because cutover and rollback evidence must reference the actual converter, CI, and backup model.

## Execution Slices

### Slice 1: Reconcile provider startup and PostgreSQL migration lineage

#### Objective
Prove which repository surfaces own provider selection, schema creation, and migration lineage so later implementation does not hide conversion behavior in the normal backend startup path.

#### Exact areas
- `OpenRestoApi/Extensions/DatabaseExtensions.cs`
- `OpenRestoApi/Program.cs`
- `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`
- `OpenRestoApi/Migrations/*`
- `OpenRestoApi.PostgresMigrations/*`
- `CLAUDE.md`
- `docs/backup-restore.md`
- `OpenRestoApi.Tests/**/*`

#### Work
1. Freeze `OpenRestoApi.PostgresMigrations` as the authoritative PostgreSQL schema lineage.
2. Identify every place where startup currently migrates or assumes provider-specific behavior.
3. Separate provider-neutral runtime startup from any future SQLite-to-PostgreSQL data-conversion command.
4. Define the implementation seam where PostgreSQL schema creation can happen without performing data import during normal app boot.
5. Record how current SQLite migration remap logic stays SQLite-only and does not bleed into PostgreSQL execution.

#### Deliverables
- Updated backend/provider notes proving which assembly owns PostgreSQL schema history.
- A concrete implementation checklist for keeping conversion out of `Database.Migrate()` startup flow.
- Explicit test targets for provider-neutral startup behavior.

#### Acceptance criteria
- The PostgreSQL migrations assembly is named as the only PostgreSQL schema authority.
- The future converter path is separated from the normal backend startup path.
- SQLite-only bootstrap/remap behavior is explicitly bounded to the SQLite provider branch.

#### Verification expectations
- Read-only repo inspection of current provider wiring and migrations assemblies.
- Planner-defined tests for provider-neutral startup and PostgreSQL migration resolution.

### Slice 2: Specify the explicit SQLite-to-PostgreSQL converter

#### Objective
Define a one-shot conversion tool contract that imports SQLite into a clean PostgreSQL target while preserving IDs, UTC semantics, foreign keys, enum/nullability behavior, and operational tables.

#### Exact areas
- New converter surface to be chosen during execution:
  - possible new utility project under repo root, or
  - a tightly scoped maintenance command adjacent to `OpenRestoApi`
- `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`
- `OpenRestoApi.PostgresMigrations/*`
- `OpenRestoApi/Core/**/*`
- `OpenRestoApi.Tests/**/*`
- `docs/backup-restore.md`
- feature-local implementation notes under `.planning/features/postgres-migration-backups/`

#### Work
1. Define the converter entrypoint, arguments, and confirmation model.
2. Require separate source and destination targets and provider checks.
3. Require refusal when:
  - source is not SQLite
  - destination is not PostgreSQL
  - source and destination resolve to the same endpoint
  - destination is nonempty
  - source migration state is unsupported or indeterminate
4. Build the PostgreSQL destination schema from `OpenRestoApi.PostgresMigrations` before import.
5. Lock deterministic dependency-safe import order for current persisted entities.
6. Preserve primary keys, foreign keys, UTC values, enums, nullable fields, and operational/admin/channel data required by current OpenResto features.
7. Reseed PostgreSQL identities/sequences after import.
8. Emit a redacted machine-readable validation report outside Git.

#### Deliverables
- A converter contract with exact preflight and refusal semantics.
- A data-domain inventory of tables/entities that must be preserved.
- A validation-report schema for conversion rehearsal evidence.

#### Acceptance criteria
- The conversion path is explicit and impossible to confuse with normal app startup.
- The destination safety gate is fail-closed on nonempty PostgreSQL targets.
- Preservation rules for IDs, UTC values, foreign keys, enums, nullable fields, and operational tables are written tightly enough that execution does not need to invent them.

#### Verification expectations
- Planned unit/integration tests for preflight refusal paths.
- Planned rehearsal tests for row counts, key invariants, and sequence reseeding.
- Planned evidence review of the emitted validation artifact.

### Slice 3: Lock PostgreSQL operational roles and SCRAM-compatible recovery rules

#### Objective
Specify least-privilege role boundaries and recovery expectations that match the repository's internal-only Docker topology and existing logical backup scripts.

#### Exact areas
- `docker-compose.postgres.yml`
- `docs/backup-restore.md`
- `scripts/postgres-backup.sh`
- `scripts/postgres-restore.sh`
- `scripts/postgres-restore-drill.sh`
- deployment env docs and examples already in repo
- future role/bootstrap SQL or docs to be added during execution

#### Work
1. Define separate PostgreSQL responsibilities for:
  - bootstrap/migration application
  - runtime application access
  - logical backup execution
  - restore-drill verification
2. Write the minimum required privileges for each role.
3. Ensure the runtime role is not the PostgreSQL superuser.
4. Keep the backup model aligned with:
  - `pg_dump --format=custom`
  - `pg_restore --list --verbose`
  - SHA-256 checksum generation and verification
5. Preserve SCRAM compatibility with `POSTGRES_INITDB_ARGS: --auth=scram-sha-256`.
6. Define what extra privileges, if any, are allowed only inside the isolated restore-drill environment.
7. Tie restore-drill evidence to the same artifact set produced by backup execution.

#### Deliverables
- Least-privilege role matrix.
- Recovery policy describing what counts as a valid backup and valid restore drill.
- Repo-aligned operator guidance for secrets placement and internal-only topology.

#### Acceptance criteria
- Bootstrap, runtime, backup, and restore-drill responsibilities are distinct and reviewable.
- The runtime role is explicitly narrower than bootstrap privileges.
- Recovery policy requires both logical archive verification and restore-drill success.

#### Verification expectations
- Planned role-based integration checks proving runtime operations succeed without superuser rights.
- Planned backup/restore script checks under SCRAM-compatible PostgreSQL setup.
- Planned drill evidence capture linked to the generated backup archive.

### Slice 4: Add PostgreSQL CI integration and provider-aware migration checks

#### Objective
Extend CI so provider-cutover confidence is based on PostgreSQL behavior and not only on SQLite migration SQL symmetry.

#### Exact areas
- `.github/workflows/ci.yml`
- `.github/workflows/migration-check.yml`
- `OpenRestoApi.Tests/**/*`
- any test compose/test harness files needed for PostgreSQL execution
- `OpenRestoApi.PostgresMigrations/*`
- `OpenRestoApi/Extensions/DatabaseExtensions.cs`

#### Work
1. Add a PostgreSQL-aware CI path using repo-supported tooling only.
2. Prove fresh PostgreSQL schema creation from the dedicated migrations assembly.
3. Prove application startup against PostgreSQL with pending migrations resolved.
4. Run representative PostgreSQL integration coverage for critical backend behavior.
5. Separate PostgreSQL provider validation from the existing SQLite migration-diff workflow rather than replacing the SQLite check outright.
6. Ensure CI evidence is reviewable without production secrets.

#### Deliverables
- A PostgreSQL CI matrix design.
- A provider-aware migration-safety check design.
- A defined representative integration-test subset that must pass under PostgreSQL before cutover rehearsal is considered.

#### Acceptance criteria
- CI no longer treats SQLite-only validation as sufficient for provider-cutover sign-off.
- PostgreSQL schema creation, startup, and representative behavior each have explicit automated gates.
- The migration-check story is clear for both SQLite and PostgreSQL providers.

#### Verification expectations
- Planned GitHub Actions runs using service containers or equivalent repo-native harnesses.
- Planned artifact/log review showing PostgreSQL schema creation and startup success.
- Planned regression proof that existing SQLite safety checks remain intact where still relevant.

### Slice 5: Define test-only cutover, rollback, and sign-off evidence

#### Objective
Write the exact test-only cutover and rollback procedure that will block unsafe promotion and force recoverability proof before PostgreSQL becomes the sole writable source anywhere.

#### Exact areas
- `docs/backup-restore.md`
- test-runbook docs under `docs/` or feature-local docs
- deployment/test environment docs already in repo
- future rehearsal evidence storage conventions to be defined in planning docs

#### Work
1. Write the test cutover checklist.
2. Require immutable pre-cutover SQLite snapshot retention.
3. Require recording of the exact release candidate or image digest used for the rehearsal.
4. Require converter validation report review before backend startup on PostgreSQL.
5. Require backend health, admin login, and representative booking-flow checks after cutover.
6. Require PostgreSQL backup creation and successful restore-drill evidence from that backup set.
7. Write the rollback checklist for failures before PostgreSQL becomes the only writable source.
8. Define the exact stop condition that makes rollback mandatory.
9. State clearly that production cutover remains out of scope until the test-only gates pass and a later feature authorizes promotion.

#### Deliverables
- Test-only cutover checklist.
- Rollback checklist against the same release candidate.
- Sign-off matrix defining required evidence and blocking conditions.

#### Acceptance criteria
- Test cutover evidence requirements are explicit and auditable.
- Rollback evidence requirements are explicit and executable before irreversible promotion.
- Production readiness language is blocked behind test-only success plus later approval.

#### Verification expectations
- Planned rehearsal record format for cutover and rollback.
- Planned evidence collection for backup, restore drill, startup, and representative flows.
- Explicit pass/fail gates for stopping the rehearsal and reverting to SQLite.

## Expected Changed Areas During Future Execution
- `OpenRestoApi/Extensions/*`
- `OpenRestoApi/Infrastructure/Persistence/*`
- `OpenRestoApi.PostgresMigrations/*`
- `OpenRestoApi.Tests/**/*`
- possible new converter project or maintenance command under the repo root
- `.github/workflows/ci.yml`
- `.github/workflows/migration-check.yml`
- `docker-compose.postgres.yml`
- `docs/backup-restore.md`
- additional repo docs or runbooks for rehearsal evidence and least-privilege roles

## Verification Gates For Execution
- Focused tests prove provider-neutral startup and PostgreSQL migration resolution.
- Converter tests prove provider mismatch, same-endpoint, and nonempty-destination refusal.
- Converter rehearsal proves row-count validation, key invariants, UTC preservation, and sequence reseeding.
- Role-based checks prove the runtime application path does not need superuser privileges.
- Backup checks prove archive creation, `pg_restore --list --verbose`, checksum generation, and restore-drill success.
- CI proves PostgreSQL schema creation, startup, and representative backend integration coverage.
- Test-only cutover rehearsal proves the exact rollback boundary before PostgreSQL becomes the sole writable source.

## Non-Goals
- No production cutover approval in this planning pass.
- No assumption that existing PostgreSQL scripts alone prove migration readiness.
- No dual-write or mixed-write-source architecture.
- No application implementation in this document.
