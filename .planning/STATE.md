# State

## Planning Status
- Onboarding completed for the brownfield repo.
- A verified codebase map exists at `.planning/codebase/CODEBASE_MAP.md`.
- This onboarding refresh corrected stale top-level planning metadata that still described older feature work rather than the repository’s current PostgreSQL migration state.
- The SQLite-to-PostgreSQL repair track is captured under `.planning/features/postgres-migration-backups/`.
- The binding design is captured at `.planning/features/internal-operator-mcp/SPEC.md`.
- The implementation plan and recorded local verification are under `.planning/features/internal-operator-mcp/`.
- A new Level-C planning set for the test-only WhatsApp reservation channel now exists under `.planning/features/whatsapp-reservations-test/`.
- The current feature branch already contains partial OpenResto-side WhatsApp channel implementation work that has been assessed against `develop` and folded into the new planning baseline.
- Phases 3 through 7 for `whatsapp-reservations-test` have now been executed in-worktree with a durable `n8n-test` foundation, explicit state storage, versioned workflow exports, contract documentation, SuperAdmin-only admin settings APIs, Expo WhatsApp settings cards, a webhook-only test edge topology, and a Meta/WABA operator runbook that marks all external work `USER/AWAITING`. No test deployment, DNS mutation, or external webhook activation has occurred.

## Implemented Decisions
- The remote Streamable HTTP MCP server lives in the existing ASP.NET Core backend at `/api/mcp/operator`.
- Durable `OperatorPrincipal` records use an `OperatorRestaurantScope` join table.
- Opaque short-lived operator credentials are stored only as digests and resolve to an operator principal.
- Escalation writes an audit record and uses the existing internal notification architecture.
- Backend provider selection now exists in-repo via `DATABASE_PROVIDER`, with `sqlite` and `postgres` branches in `DatabaseExtensions`.
- A dedicated PostgreSQL migrations assembly, PostgreSQL release overlay, and PostgreSQL backup/restore scripts are present in the repository, but end-to-end cutover safety is not yet proven by this planning pass.

## Inspection Evidence From This Pass
- `OpenRestoApi/Extensions/DatabaseExtensions.cs` now wires both SQLite and Npgsql paths and points PostgreSQL migrations at `OpenRestoApi.PostgresMigrations`.
- `OpenRestoApi.PostgresMigrations/` contains a generated PostgreSQL baseline migration assembly.
- `docker-compose.postgres.yml` enforces internal-only PostgreSQL networking and `POSTGRES_INITDB_ARGS: --auth=scram-sha-256`.
- `scripts/postgres-backup.sh`, `scripts/postgres-restore.sh`, and `scripts/postgres-restore-drill.sh` provide logical dump, checksum, restore, and disposable drill mechanics.
- `.github/workflows/ci.yml` and `.github/workflows/migration-check.yml` still center SQLite-backed validation; no dedicated PostgreSQL CI cutover/integration gate was found during inspection.
- No application code, tests, scripts, or deployment files were changed in this pass; only planning artifacts were updated.

## Next Recommended Step
- For `postgres-migration-backups`, run a discuss/plan pass that turns the now-explicit conversion, least-privilege role, SCRAM backup/restore-drill, CI PostgreSQL integration, and test-only cutover/rollback gates into an executable implementation plan without touching production.
- The MCP and WhatsApp planning tracks remain as separate workstreams and should not be conflated with the PostgreSQL migration repair.
