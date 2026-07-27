# Internal Operator MCP Verification

Date: 2026-07-27 UTC

## Commands
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet build OpenRestoApi/OpenRestoApi.csproj -c Release`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet vstest OpenRestoApi.Tests/bin/Debug/net10.0/OpenRestoApi.Tests.dll /TestCaseFilter:FullyQualifiedName~OperatorMcpFoundationMigrationTests`
- Live smoke:
  `docker run -d --name openresto-mcp-smoke -p 18080:18080 -v /tmp/openresto-internal-mcp-design:/src -w /src/OpenRestoApi ... mcr.microsoft.com/dotnet/sdk:10.0 bash -lc 'dotnet run'`
  plus a live DB seeder and a live MCP client against `http://127.0.0.1:18080/api/mcp/operator`

Raw logs:
- `verification/release-build.log`
- `verification/full-test.log`
- `verification/migration-vstest.log`
- `verification/mcp-smoke.log`
- `verification/mcp-smoke-server.log`

## Results
- Release build: passed, `0` errors, `39` warnings.
- Full automated test suite: passed, `1194` passed, `0` failed, `0` skipped.
- Focused migration verification: passed, `2` passed, `0` failed.
- Coverage from full suite: line `96.76%`, branch `84.65%`, method `98.25%`.

## Migration Proof
- `OperatorMcpFoundationMigrationTests.FreshInstall_CreatesOperatorTables_AndNullableBookingOwnershipColumns`: passed.
- `OperatorMcpFoundationMigrationTests.Upgrade_ProducesSameBookingsSchema_AsFreshInstall`: passed.

## MCP Smoke
- Health probe: `GET /api/health` returned `200 {"status":"ok"}`.
- Tool discovery over authenticated MCP returned:
  `operator_cancel_reservation`
  `operator_create_reservation`
  `operator_escalate_reservation`
  `operator_get_availability`
  `operator_get_reservation`
  `operator_list_reservations`
  `operator_update_reservation`
- Authenticated tool invocation:
  `operator_list_reservations` returned `STRUCTURED=[]` with no MCP error flag.

## Notes
- No nginx or Compose route change was required for this feature because the existing `/api/` reverse-proxy rule already covers `/api/mcp/operator`.
- The MCP endpoint rate limit remains enabled outside the `Testing` environment. It is disabled only for the test host because the SDK transport performs extra initialization requests that make deterministic MCP-throttle assertions unreliable in the shared integration factory; credential-partition throttling is still verified on the operator HTTP route that uses the same `operatorMcp` policy.
- Existing dependency warnings remain for `Microsoft.OpenApi` `2.0.0` and `SQLitePCLRaw.lib.e_sqlite3` `2.1.11`. They were present during verification and were not changed by this feature.
