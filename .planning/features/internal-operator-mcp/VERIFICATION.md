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

## 2026-07-27 SuperAdmin Settings Credential Management Slice

Commands:
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~OperatorCredentialManagementServiceTests|FullyQualifiedName~AdminOperatorCredentialsControllerTests|FullyQualifiedName~RoleAuthorizationTests|FullyQualifiedName~AuthGateTests"`
- `docker run --rm -v /tmp/openresto-internal-mcp-design:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
- `npm --prefix openresto-frontend ci`
- `npm --prefix openresto-frontend run check`
- `npx --prefix openresto-frontend tsc --noEmit -p openresto-frontend/tsconfig.json`
- `npm --prefix openresto-frontend test -- --runInBand tests/app/admin/settings.test.tsx tests/components/admin/settings/OperatorCredentialsCard.test.tsx tests/api/admin.test.ts`
- `npx --prefix openresto-frontend prettier --check openresto-frontend/app/admin/settings.tsx openresto-frontend/api/admin.ts openresto-frontend/components/admin/settings/OperatorCredentialsCard.tsx openresto-frontend/components/admin/settings/UsersRolesCard.tsx openresto-frontend/components/admin/settings/settings.styles.ts openresto-frontend/tests/app/admin/settings.test.tsx openresto-frontend/tests/api/admin.test.ts openresto-frontend/tests/components/admin/settings/OperatorCredentialsCard.test.tsx`
- `npx --prefix openresto-frontend oxlint openresto-frontend/app/admin/settings.tsx openresto-frontend/api/admin.ts openresto-frontend/components/admin/settings/OperatorCredentialsCard.tsx openresto-frontend/components/admin/settings/UsersRolesCard.tsx openresto-frontend/components/admin/settings/settings.styles.ts openresto-frontend/tests/app/admin/settings.test.tsx openresto-frontend/tests/api/admin.test.ts openresto-frontend/tests/components/admin/settings/OperatorCredentialsCard.test.tsx`

Results:
- Focused backend suite in `mcr.microsoft.com/dotnet/sdk:10.0`: passed, `55` passed, `0` failed, `0` skipped, duration `6 s`.
- Full backend suite in `mcr.microsoft.com/dotnet/sdk:10.0`: passed, `1214` passed, `0` failed, `0` skipped, duration `28 s`.
- Full backend coverage after the slice: line `96.56%`, branch `84.39%`, method `98.13%`.
- Frontend dependency install: passed, `972` packages added; `npm audit` reported `4 high severity vulnerabilities` already present in the dependency graph.
- Frontend repo-wide `npm run check`: failed on pre-existing formatting drift outside this slice only:
  `components/layout/LanguageSelector.tsx`
  `i18n/locale.ts`
  `i18n/messages.ts`
  `tests/context/I18nContext.test.tsx`
  `tests/i18n/locale.test.ts`
- Frontend typecheck: passed, exit code `0`.
- Relevant frontend Jest suite: passed, `3` suites, `130` tests, `0` failed.
- Slice-specific frontend formatting check: passed.
- Slice-specific frontend lint (`oxlint`) on changed files: passed.

Behavioral proof:
- SuperAdmin-only backend routes added under `/api/admin/operator-credentials`; `RoleAuthorizationTests` covers SuperAdmin allow and BookingEditor deny.
- Issue flow normalizes the operator identifier, requires at least one real restaurant scope, enforces bounded TTL (`default 8h`, `max 24h`) and bounded notes, reuses or creates the `OperatorPrincipal`, and returns the plaintext bearer only in the create response.
- List flow returns only non-secret metadata (`identifier`, restaurant scopes, `credentialKeyId`, issue/expiry/revocation/last-used timestamps, notes).
- Revoke flow immediately blocks reuse of the old bearer token through the existing operator bearer authentication path; verified by `AdminOperatorCredentialsControllerTests.SuperAdmin_Can_Issue_List_And_Revoke_OperatorCredential`.
- No migration was added because the existing operator principal / scope / credential schema already supported the required slice.
