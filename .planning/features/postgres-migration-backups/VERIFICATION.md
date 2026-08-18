# PostgreSQL Migration Repair — Verification Matrix

## Evidence gathered in this planning-only pass
- [x] Repository inspection completed for provider wiring, PostgreSQL migrations assembly, release overlay, backup/restore scripts, and CI workflows.
- [x] `DATABASE_PROVIDER` switching and PostgreSQL migrations assembly wiring were verified in `OpenRestoApi/Extensions/DatabaseExtensions.cs`.
- [x] SCRAM-compatible PostgreSQL initialization was verified in `docker-compose.postgres.yml`.
- [x] Logical backup, checksum, restore, and restore-drill scripts were verified as present in `scripts/postgres-backup.sh`, `scripts/postgres-restore.sh`, and `scripts/postgres-restore-drill.sh`.
- [x] Existing runbook coverage was verified in `docs/backup-restore.md`.
- [x] SQLite-centric CI emphasis was verified in `.github/workflows/ci.yml` and `.github/workflows/migration-check.yml`.

## Not executed in this pass
- [ ] No application code was modified.
- [ ] No automated tests were run.
- [ ] No backup or restore commands were executed.
- [ ] No conversion dry run or cutover rehearsal was performed.
- [ ] No deployment, commit, or push was performed.

## Open implementation gates

### Conversion tool
- [ ] Explicit SQLite-to-PostgreSQL conversion command/tool is implemented and reviewable.
- [ ] Tool refuses dirty or non-empty PostgreSQL destinations.
- [ ] Tool preserves keys, UTC values, enums, nullable values, and current OpenResto operational tables.
- [ ] Tool reseeds PostgreSQL identities/sequences after import.
- [ ] Tool emits a redacted validation report outside Git.

### Least-privilege roles
- [ ] Bootstrap/migration, runtime, backup, and restore-drill roles are specified and implemented separately.
- [ ] Runtime application connection avoids PostgreSQL superuser credentials.
- [ ] Backup and restore-drill credentials are compatible with least-privilege expectations.

### SCRAM backup and restore verification
- [ ] Backup verification proves `pg_dump --format=custom`, `pg_restore --list --verbose`, and checksum generation together.
- [ ] Restore drill proves archive usability in an isolated disposable PostgreSQL target.
- [ ] Restore-drill evidence is recorded as a required acceptance artifact, not an optional operator note.

### CI PostgreSQL integration
- [ ] CI creates a fresh PostgreSQL schema from the PostgreSQL migrations assembly.
- [ ] CI proves application startup against PostgreSQL.
- [ ] CI runs representative PostgreSQL integration coverage.
- [ ] CI no longer relies exclusively on SQLite migration safety for provider-cutover sign-off.

### Test-only cutover and rollback
- [ ] Test cutover checklist exists with immutable SQLite snapshot, release-candidate identity, validation report, health checks, representative flow checks, and restore-drill evidence.
- [ ] Rollback checklist exists with retained SQLite snapshot and exact stop/restart criteria.
- [ ] Promotion policy explicitly blocks production cutover until the test-only acceptance criteria pass.
