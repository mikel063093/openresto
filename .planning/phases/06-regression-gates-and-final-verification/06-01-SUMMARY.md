# Plan 06-01 Summary

## Completed Work

- Added a narrow regression gate in `scripts/check-i18n-regressions.cjs` backed by `i18n/regression-allowlists.json` so Phase 6 enforces stable product-copy seams instead of blanket string scanning.
- Localized remaining shared visible frontend seams that still emitted raw English product copy, including the not-found route, shared user route titles, hold-status copy, and restaurant location metadata labels.
- Localized backend user-facing controller messages for brand settings, email settings, and media validation through `ApiLocalization`, including an explicit dynamic allowlist for connection-failure detail text.
- Added focused regression coverage for the new frontend and backend localized paths in both `en` and `es-CO`.

## Evidence

- `scripts/check-i18n-regressions.cjs`
- `i18n/regression-allowlists.json`
- `openresto-frontend/i18n/messages.ts`
- `openresto-frontend/app/+not-found.tsx`
- `openresto-frontend/app/(user)/_layout.tsx`
- `openresto-frontend/app/(user)/locations/index.tsx`
- `openresto-frontend/components/booking/HoldStatusBanner.tsx`
- `openresto-frontend/components/restaurant/LocationListItem.tsx`
- `OpenRestoApi/Infrastructure/Localization/ApiLocalization.cs`
- `OpenRestoApi/Controllers/BrandController.cs`
- `OpenRestoApi/Controllers/EmailSettingsController.cs`
- `OpenRestoApi/Controllers/MediaController.cs`
- `openresto-frontend/tests/app/not-found.test.tsx`
- `openresto-frontend/tests/components/HoldStatusBanner.test.tsx`
- `OpenRestoApi.Tests/Controllers/EmailSettingsControllerUnitTests.cs`
- `OpenRestoApi.Tests/Controllers/MediaControllerUnitTests.cs`
- `OpenRestoApi.Tests/Integration/BrandControllerTests.cs`

## Verification

- `node scripts/check-i18n-regressions.cjs`
- `npm test --prefix openresto-frontend -- --runInBand tests/app/not-found.test.tsx tests/components/HoldStatusBanner.test.tsx`
- `docker run --rm -v /tmp/openresto-i18n-audit:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~BrandControllerTests|FullyQualifiedName~EmailSettingsControllerUnitTests|FullyQualifiedName~MediaControllerUnitTests"`
