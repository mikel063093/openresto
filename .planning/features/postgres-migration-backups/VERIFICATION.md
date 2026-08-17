# PostgreSQL Migration and Verified Backups — Verification Matrix

## Code/provider
- [ ] Targeted provider selection and startup tests pass.
- [ ] Full backend test suite passes with actual discovered/passed/failed counts.
- [ ] Fresh PostgreSQL schema is created by EF migrations and records expected history.
- [ ] No SQLite PRAGMA/catalog SQL reaches the PostgreSQL provider.

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
