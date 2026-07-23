# Phase 06 UAT

## Runtime Checks

- Public runtime on `http://localhost:5062` switched from English to Spanish with the language selector, stayed in Spanish after reload, and preserved the authored restaurant name `Pasta Place` unchanged.
- Admin runtime on `http://localhost:5062/admin/dashboard` honored persisted `es-CO` locale state, rendered Spanish dashboard chrome, and stayed in Spanish after reload.
- Public and admin smoke checks passed from Playwright on the isolated source-built stack:
  - `npx playwright test --project=chromium e2e/home.spec.ts`
  - `npx playwright test --project=chromium-admin e2e/admin-dashboard.spec.ts`

## Contract And Preservation Notes

- Tenant-authored restaurant content remained verbatim in the live stack while product-owned navigation, labels, and status copy localized.
- Locale persistence was verified through the browser-facing runtime.
- `Accept-Language` propagation and localized backend message contracts were verified by the passing automated contract tests already included in the final suite:
  - `openresto-frontend/tests/api/client.test.ts`
  - `OpenRestoApi.Tests/Controllers/EmailSettingsControllerUnitTests.cs`
  - `OpenRestoApi.Tests/Controllers/MediaControllerUnitTests.cs`
  - `OpenRestoApi.Tests/Integration/BrandControllerTests.cs`
