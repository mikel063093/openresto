# PostgreSQL Migration and Verified Backups — Context

## User direction
- Migrate OpenResto SQLite to PostgreSQL to support production-grade migrations and automatic backups.
- Add root `CLAUDE.md` guidance where database connection/migration work is described, including what to do when a data value/schema changes.
- Proceed autonomously, with test-first execution and real verification.

## Directly verified runtime state
- `test-rest-backend-1` and `mike-openresto-backend-1` run `ASPNETCORE_ENVIRONMENT=Production` and `CONNECTION_STRING=Data Source=/data/openresto.db`.
- Test SQLite volume: `test-rest_test_rest_db_data`; production SQLite volume: `mike-openresto_openresto_db_data`.
- Test source DB: 288 KB, 2 restaurants, 0 bookings, 1 admin credential, current migration history through WhatsApp additions.
- Production source DB: 124 KB, 2 restaurants, 4 bookings, 1 admin credential, 17 migration rows, integrity check `ok`.
- Repository `DatabaseExtensions` currently calls `UseSqlite`, SQLite `PRAGMA`, SQLite catalog commands and `SqliteRetryingExecutionStrategy`; these require provider separation.
- Normal startup executes `Database.Migrate()` and seeding.
- Existing backup document contains SQLite examples only; filesystem search did not find an enabled OpenResto backup timer/job.

## Deployment boundary
- Base branch is `develop`, because it matches active test feature development and has the current schema. The primary `main` worktree contains unrelated user edits and must not be changed.
- Implementation occurs in `/tmp/openresto-postgres` on `feat/postgres-migration-backups`.
- No secret values are to be committed or echoed. External backup destination is configured only at deploy time.

## Decision
- Dedicated Postgres service/volume/user per OpenResto environment; do not reuse Dokploy's internal Postgres.
- Implement actual database/provider code, conversion tooling, compose/test configuration, backup and restore verification, runbook, and CLAUDE guidance before test deployment.
