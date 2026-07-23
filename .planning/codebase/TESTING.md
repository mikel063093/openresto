# Testing Patterns

**Analysis Date:** 2026-07-23

## Test Framework

**Frontend Runner:**
- Jest via `jest-expo`
- Config is embedded in `openresto-frontend/package.json`

**Backend Runner:**
- .NET test project under `OpenRestoApi.Tests`
- Broad unit + integration coverage organized by feature/infrastructure area

**E2E:**
- Playwright under `openresto-frontend/e2e`

**Run Commands:**
```bash
dotnet test
cd openresto-frontend && npm test
cd openresto-frontend && npm test -- --coverage
cd openresto-frontend && npm run test:e2e
```

## Test File Organization

**Frontend:**
- Separate `openresto-frontend/tests/` tree
- Route tests under `tests/app/**`
- Component tests under `tests/components/**`
- API tests under `tests/api/**`
- Context/hooks/utils tests under dedicated subfolders

**Backend:**
- `OpenRestoApi.Tests/Controllers`, `Services`, `Integration`, `Infrastructure`, `Migrations`, `Utilities`, etc.

## Relevant Existing Localization Coverage

- `openresto-frontend/tests/i18n/locale.test.ts`
- `openresto-frontend/tests/context/I18nContext.test.tsx`
- `openresto-frontend/tests/components/layout/LanguageSelector.test.tsx`
- `openresto-frontend/tests/utils/formatters.test.ts`

These indicate partial localization work has already been introduced and should be extended rather than replaced.

## Patterns

- Frontend tests use rendered components/providers and endpoint mocking patterns already established across the repo.
- Backend tests cover controller responses, service behavior, and full integration flows through `TestWebAppFactory`.
- E2E specs already cover guest/admin flows that localization should reuse as verification gates instead of inventing entirely new end-to-end coverage.

## Coverage

- Frontend Jest config collects coverage from app/components/api/context/hooks/utils.
- The repo README states a 100% frontend coverage target, so localization work should assume scrutiny on added branches.
- Backend has extensive breadth even if no explicit percentage target is surfaced in the inspected files.

## Localization Test Strategy Implications

- Prefer focused bilingual assertions in existing route/component tests for deterministic coverage.
- Add representative backend integration tests keyed by `Accept-Language` for both `en` and `es-CO`.
- Use existing Playwright guest/admin flows as smoke gates for locale switching and localized critical-path behavior.
- Add narrow regression detection for newly introduced untranslated strings rather than broad brittle snapshot baselines.

---
*Testing analysis: 2026-07-23*
*Update when test patterns change*
