# PostgreSQL Migration and Verified Backups — Verification Matrix

## Code/provider
- [x] Targeted provider/startup tests passed: `17 passed, 0 failed` (DatabaseExtensions + InitializeDatabase focused suite).
- [ ] Full backend test suite has not yet been rerun after the provider commit.
- [ ] **BLOCKED:** Fresh PostgreSQL schema is not created. An isolated PostgreSQL 16 startup verification failed before any schema was written because EF Core reported `PendingModelChangesWarning`; the existing migrations/model snapshot were generated for SQLite and cannot be used as the PostgreSQL migration lineage.
- [x] Provider selection gates SQLite-only PRAGMA/catalog/WAL/remap code behind `DatabaseProvider.Sqlite`; it is not entered for the PostgreSQL startup path.

**Required next implementation:** introduce a dedicated PostgreSQL migrations assembly/baseline generated under the Npgsql provider, wire `UseNpgsql(..., x => x.MigrationsAssembly(...))`, and add an automated fresh PostgreSQL migration integration test. Only then can the SQLite-to-PostgreSQL converter and a test cutover be safely implemented.

## Converter
- [ ] Representative SQLite fixture imports into a clean Postgres destination.
- [ ] Source artifact stays read-only/unchanged.
- [ ] Row counts, IDs, natural keys, migration history, and UTC values match.
- [ ] PostgreSQL sequences are reseeded.
- [ ] A non-empty destination conversion is refused.

## Backup/recovery
- [ ] `pg_dump` custom archive completes.
- [ ] `pg_restore --list` and checksum pass.
- [ ] Archive is encrypted/uploaded only when credentials are injected.
- [ ] Restore drill creates a disposable DB and validates rows/migrations.
- [ ] Failure/stale-backup alert path is executable without exposing secrets.

## Deployment
- [ ] Compose topology has no host-mapped Postgres port.
- [ ] Test cutover has snapshot, conversion report, health checks and restore evidence.
- [ ] Production has an independently verified snapshot and rollback plan before any write.
- [ ] Old SQLite volume is retained for agreed rollback period.
