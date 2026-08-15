# Phase 06 Verification

## Status

Phase 6 completed on 2026-07-23.

## Automated Evidence

- `node scripts/check-i18n-regressions.cjs`
  Result: passed.
- `npm test --prefix openresto-frontend -- --runInBand`
  Result: `150` suites passed, `1925` tests passed.
- `npm run check --prefix openresto-frontend`
  Result: passed; formatter and `oxlint` completed successfully, with existing warnings only.
- `npm exec --prefix openresto-frontend tsc -- --noEmit -p openresto-frontend/tsconfig.json`
  Result: passed.
- `npx expo export --platform web`
  Result: passed; static export completed to `openresto-frontend/dist`.
- `docker run --rm -v /tmp/openresto-i18n-audit:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
  Result: `1209` tests passed.
- `npx playwright test --project=chromium e2e/home.spec.ts`
  Result: `3` tests passed.
- `npx playwright test --project=chromium-admin e2e/admin-dashboard.spec.ts`
  Result: `4` tests passed.

## Verification Notes

- The regression gate is intentionally narrow and allowlist-driven so it catches newly introduced untranslated product copy and backend user-facing message gaps without flagging tenant-authored content or unrelated technical literals.
- Frontend and backend localized contract coverage now proves both `en` and `es-CO` behavior for visible UI strings, backend `{ message }` responses, and persisted locale selection.
- Runtime smoke checks confirmed effective Spanish behavior on the live stack and preserved authored restaurant content.
- Backend verification continued to use the .NET 10 SDK container because the host workspace does not expose a local `dotnet` binary.
