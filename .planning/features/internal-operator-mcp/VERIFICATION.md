# Internal Operator MCP Verification

Date: 2026-07-27 UTC

## Commands
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~BookingServiceTests|FullyQualifiedName~OperatorReservationServiceTests|FullyQualifiedName~OperatorReservationOwnershipIntegrationTests|FullyQualifiedName~OperatorAvailabilityIntegrationTests|FullyQualifiedName~OperatorMcpFoundationMigrationTests"`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src/OpenRestoApi mcr.microsoft.com/dotnet/sdk:10.0-preview sh -lc "dotnet tool install --tool-path /tmp/dotnet-tools dotnet-ef --version 10.0.10 >/tmp/install.log && /tmp/dotnet-tools/dotnet-ef migrations add AddOperatorAuditRetentionAndEscalationDurability"`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`

Raw logs:
- `verification/full-test.log`

## Results
- Targeted RED/GREEN regression suite: passed, `66` passed, `0` failed, `0` skipped.
- Additive migration/snapshot: generated as `20260727062349_AddOperatorAuditRetentionAndEscalationDurability`.
- Full automated backend suite: passed, `1203` passed, `0` failed, `0` skipped.
- Coverage from full suite: line `96.51%`, branch `84.13%`, method `98.17%`.

## Migration Proof
- `OperatorMcpFoundationMigrationTests.FreshInstall_CreatesOperatorTables_AndNullableBookingOwnershipColumns`: passed.
- `OperatorMcpFoundationMigrationTests.Upgrade_ProducesSameBookingsSchema_AsFreshInstall`: passed.
- `OperatorMcpFoundationMigrationTests.FreshInstall_OperatorActionAudits_RetainRows_WhenOperatorOrRestaurantDeleted`: passed.
- `OperatorMcpFoundationMigrationTests.Upgrade_PreservesOperatorActionAuditDeleteSemantics_AsFreshInstall`: passed.

## Notes
- The strict TDD loop ran in a containerized SDK because this workspace does not expose a local `dotnet` binary on `PATH`.
- A pre-existing compile blocker in `ForwardedHeadersOptions.KnownIPNetworks` had to be corrected to `KnownNetworks` before the requested tests could execute under the available SDK.
- Existing dependency warnings remain for `Microsoft.OpenApi` `2.0.0` and `SQLitePCLRaw.lib.e_sqlite3` `2.1.11`. They were present during verification and were not changed by this feature.
