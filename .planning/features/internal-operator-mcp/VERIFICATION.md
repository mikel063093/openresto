# Internal Operator MCP Verification

Date: 2026-07-27 UTC

## Commands
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~BookingServiceTests|FullyQualifiedName~OperatorReservationServiceTests|FullyQualifiedName~OperatorReservationOwnershipIntegrationTests|FullyQualifiedName~OperatorAvailabilityIntegrationTests|FullyQualifiedName~OperatorMcpFoundationMigrationTests"`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src/OpenRestoApi mcr.microsoft.com/dotnet/sdk:10.0-preview sh -lc "dotnet tool install --tool-path /tmp/dotnet-tools dotnet-ef --version 10.0.10 >/tmp/install.log && /tmp/dotnet-tools/dotnet-ef migrations add AddOperatorAuditRetentionAndEscalationDurability"`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`

Raw logs:
- `verification/full-test.log`

## Results
- Targeted RED/GREEN regression suite: passed, `66` passed, `0` failed, `0` skipped.
- Additive migration/snapshot: generated as `20260727062349_AddOperatorAuditRetentionAndEscalationDurability`.
- Full automated backend suite: passed, `1203` passed, `0` failed, `0` skipped.
- Coverage from full suite: line `96.51%`, branch `84.13%`, method `98.17%`.
- Independent re-verification with the exact non-preview SDK command above: passed, `1203` passed, `0` failed, `0` skipped, exit code `0`.

## Migration Proof
- `OperatorMcpFoundationMigrationTests.FreshInstall_CreatesOperatorTables_AndNullableBookingOwnershipColumns`: passed.
- `OperatorMcpFoundationMigrationTests.Upgrade_ProducesSameBookingsSchema_AsFreshInstall`: passed.
- `OperatorMcpFoundationMigrationTests.FreshInstall_OperatorActionAudits_RetainRows_WhenOperatorOrRestaurantDeleted`: passed.
- `OperatorMcpFoundationMigrationTests.Upgrade_PreservesOperatorActionAuditDeleteSemantics_AsFreshInstall`: passed.

## Notes
- The strict TDD loop ran in a containerized SDK because this workspace does not expose a local `dotnet` binary on `PATH`.
- An independent run of `mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj` initially failed at compile time with `CACC000` accessibility-analyzer errors in `OpenRestoApi.Tests/Services/OperatorReservationServiceTests.cs`.
- The fix was narrow and repo-consistent: explicit `[OnlyAccessibleBy("OpenRestoApi.Tests.Services.OperatorReservationServiceTests")]` annotations were added to `BookingRepository`, `TableRepository`, `SectionRepository`, `RestaurantRepository`, and `HoldService`, matching the existing restricted-access convention used by neighboring tests.
- A pre-existing compile blocker in `ForwardedHeadersOptions.KnownIPNetworks` had to be corrected to `KnownNetworks` before the requested tests could execute under the available SDK.
- Existing dependency warnings remain for `Microsoft.OpenApi` `2.0.0` and `SQLitePCLRaw.lib.e_sqlite3` `2.1.11`. They were present during verification and were not changed by this feature.

## 2026-07-27 Availability Audit FK Regression

Commands:
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter OperatorAvailabilityIntegrationTests`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`

Results:
- RED before fix: `ScopedOperatorCredential_RequestingNonexistentRestaurant_ReturnsNotFound_AndAuditsWithoutInvalidRestaurantFk` failed with `Expected: NotFound`, `Actual: InternalServerError`.
- GREEN targeted regression suite after fix: `3` passed, `0` failed, `0` skipped.
- Exact required full suite after fix: `1205` passed, `0` failed, `0` skipped, duration `27 s`.
- Full-suite coverage after fix: line `96.51%`, branch `84.26%`, method `98.17%`.

Audit evidence:
- Nonexistent requested restaurant ID: response remained `404`; denial audit stored `RestaurantId = null`, `RestaurantIdSnapshot = <requested id>`, `RestaurantNameSnapshot = ""`.
- Existing but out-of-scope restaurant ID: response remained `404`; denial audit stored a valid `RestaurantId` FK to the existing restaurant plus the same durable requested-id snapshot.
