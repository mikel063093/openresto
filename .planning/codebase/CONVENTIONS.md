# Conventions

**Analysis Date:** 2026-07-23

## Code Organization Conventions

- Frontend imports use the `@/` alias extensively.
- Expo Router file-system routing determines public/admin page structure.
- Backend separates controllers, services, domain models, infrastructure, and extension wiring in a conventional layered layout.

## Testing Conventions

- Frontend tests mirror route/component structure under `openresto-frontend/tests/`.
- Backend tests are grouped by concern (`Controllers`, `Services`, `Integration`, `Infrastructure`, `Migrations`, `Utilities`).
- Existing localization work already added `tests/context/I18nContext.test.tsx`, `tests/i18n/locale.test.ts`, and `tests/components/layout/LanguageSelector.test.tsx`.

## Localization-Specific Conventions Already Present

- Frontend locale type is currently `Locale = "en" | "es"`.
- Message lookup is typed through `translate(locale, key, values?)` in `openresto-frontend/i18n/messages.ts`.
- `normalizeLocale()` already folds browser values such as `es-CO` into the current Spanish bucket.

## Brownfield Conventions To Preserve

- Do not change tenant-authored data into catalog-managed strings.
- Do not change response shape away from existing `{ message }` bodies.
- Prefer central seams over scattered one-off localization logic:
  - frontend: context + shared API/formatter helpers
  - backend: request-culture resolution + shared message lookup
- Add tests alongside the existing domain/route/component grouping rather than inventing a separate localization-only structure for everything.

## Gaps / Drift Risks

- Many files still format dates with `undefined` locale directly, which conflicts with the active-locale convention implied by the i18n provider.
- Existing `Locale = "en" | "es"` code will need a deliberate `es-CO` representation strategy without breaking the current normalized behavior.
- Prior bilingual work may have introduced partial conventions that later plans must reconcile instead of overwrite.

---
*Convention analysis: 2026-07-23*
*Update when team conventions materially change*
