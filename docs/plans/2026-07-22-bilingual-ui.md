# English and Spanish UI Implementation Plan

> **For Hermes:** Implement this plan with strict TDD, one vertical slice at a time.

**Goal:** Make OpenResto's static customer and admin interface available in high-quality English and Spanish, selecting the browser's preferred language by default and preserving an explicit user choice.

**Architecture:** Add a lightweight, dependency-free i18n provider to the Expo frontend. The provider resolves `es` when the browser/device language preference begins with `es`; otherwise it resolves `en`. A persisted explicit setting overrides automatic detection. Components consume a typed `t()` function and `locale` for accessible labels, titles, interpolated copy, plural-aware labels, and locale-aware date formatting. API requests propagate `Accept-Language` so the backend can adopt localized responses independently.

**Tech Stack:** Expo Router, React 19, React Native Web, TypeScript, Jest, Playwright, localStorage through `StorageService`.

**Audit baseline (2026-07-22):**
- No i18n or locale provider currently exists.
- 23 public-facing files contain approximately 80 static copy candidates.
- 41 admin files contain approximately 207 static copy candidates.
- Backend has approximately 254 user-facing response/error copy sites and does not currently read `Accept-Language`.
- Tenant-authored values (restaurant name, descriptions, highlight text, custom brand subtitle/footer, tags, social labels, menu content) must remain as authored and must not be machine-translated by the product.

---

### Task 1: Define locale resolution and persistence

**Objective:** Reliably choose Spanish from the user's browser/device language preference while allowing an explicit override.

**Files:**
- Create: `openresto-frontend/i18n/locale.ts`
- Create: `openresto-frontend/context/I18nContext.tsx`
- Create test: `openresto-frontend/tests/i18n/locale.test.ts`
- Create test: `openresto-frontend/tests/context/I18nContext.test.tsx`
- Modify: `openresto-frontend/app/_layout.tsx`

**Steps:**
1. Write failing tests for `es`, `es-CO`, unsupported locales, stored `en`/`es`, and clearing the override.
2. Implement `Locale = "en" | "es"`, a safe browser-language resolver, and storage key `openresto-language`.
3. Add `I18nProvider`, `useI18n`, and `setLocale`/`useBrowserLanguage` semantics.
4. Mount `I18nProvider` above the theme and route tree.
5. Run the new Jest tests and the full frontend unit suite.

### Task 2: Create reviewed copy catalogs and formatting helpers

**Objective:** Centralize reviewed static product copy, interpolation, pluralization, and date locale selection.

**Files:**
- Create: `openresto-frontend/i18n/messages.ts`
- Create: `openresto-frontend/i18n/en.ts`
- Create: `openresto-frontend/i18n/es.ts`
- Modify: `openresto-frontend/utils/formatters.ts`
- Test: `openresto-frontend/tests/i18n/messages.test.ts`
- Test: `openresto-frontend/tests/utils/formatters.test.ts`

**Steps:**
1. Write failing tests for a Spanish navigation string, interpolation, singular/plural location labels, and Spanish date output.
2. Add typed message keys; English is the source copy and Spanish is reviewed product Spanish (neutral Latin-American register).
3. Expose `t(key, values?)` with deterministic fallback to English only for intentionally missing development keys.
4. Make date/time formatters accept the active locale rather than the browser default implicitly.
5. Run unit tests and format/lint checks.

### Task 3: Make language choice discoverable and accessible

**Objective:** Let customers and administrators change the active language from every primary navigation context.

**Files:**
- Create: `openresto-frontend/components/layout/LanguageSelector.tsx`
- Modify: `openresto-frontend/components/layout/Navbar.tsx`
- Modify: `openresto-frontend/components/layout/OverflowMenu.tsx`
- Modify: `openresto-frontend/components/layout/AdminSidebar.tsx`
- Modify: `openresto-frontend/app/admin/login.tsx`
- Test: `openresto-frontend/tests/components/layout/LanguageSelector.test.tsx`

**Steps:**
1. Write failing tests proving the selector reflects auto-detected Spanish, changes to English, and persists the override.
2. Add labelled English/Spanish options, native labels, and accessibility labels in both languages.
3. Render the selector in the public navigation/overflow surface and on the unauthenticated admin login page; preserve keyboard and mobile behavior.
4. Run focused tests plus Playwright smoke coverage for a language switch.

### Task 4: Migrate customer booking experience copy

**Objective:** Translate the complete static customer journey without changing restaurant-authored content.

**Files:**
- Modify: `openresto-frontend/app/(user)/**/*.tsx`
- Modify: `openresto-frontend/components/booking/**/*.tsx`
- Modify: `openresto-frontend/components/restaurant/**/*.tsx`
- Modify: `openresto-frontend/components/layout/{Navbar,Footer,OverflowMenu}.tsx`
- Modify: `openresto-frontend/components/common/{AlertModal,ConfirmModal,ErrorScreen,KeyboardShortcutsHelp,LinkedText,LoadingScreen,ScrollToTopFab,Select}.tsx`
- Update corresponding `openresto-frontend/tests/**`

**Steps:**
1. Migrate navigation, search, locations, restaurant details, availability, booking, confirmation, lookup, errors, labels, placeholders, empty states, modal copy, and accessible labels.
2. Preserve data received from API as tenant-authored content.
3. Add focused tests for public critical paths rendered in both locales.
4. Verify browser titles and all user-facing dates follow the selected locale.

### Task 5: Migrate admin copy and role-sensitive UI

**Objective:** Translate all static admin pages, forms, modals, status badges, validation presentation, keyboard help, navigation, and browser titles.

**Files:**
- Modify: `openresto-frontend/app/admin/**/*.tsx`
- Modify: `openresto-frontend/components/admin/**/*.tsx`
- Modify: `openresto-frontend/components/layout/AdminSidebar.tsx`
- Update corresponding `openresto-frontend/tests/**`

**Steps:**
1. Migrate dashboard, login, booking operations, locations, notifications, settings, SMTP, users/roles, and all accessibility labels.
2. Use interpolation for counts and names; do not concatenate language-specific fragments.
3. Translate static role descriptions while retaining canonical API role identifiers (`SuperAdmin`, `BookingEditor`, `BookingViewer`) in logic.
4. Run unit tests and E2E tests in both English and Spanish.

### Task 6: Propagate client locale and localize backend API responses

**Objective:** Make user-visible API response/error copy honor `Accept-Language` while preserving stable machine-readable status/data contracts.

**Files:**
- Modify: `openresto-frontend/api/*.ts`
- Create: `OpenRestoApi/Infrastructure/Localization/*`
- Modify: `OpenRestoApi/Program.cs` and user-facing controllers/services
- Create/modify: `OpenRestoApi.Tests/**`

**Steps:**
1. Add an HTTP client helper that sends the current locale as `Accept-Language`.
2. Write failing integration tests for representative booking/auth/hold errors with `en` and `es` headers.
3. Add request-culture resolution and central catalog/resource lookup for user-facing server messages; preserve JSON keys and HTTP status codes.
4. Localize transactional email templates/subjects and notification copy using the request/booking language when available; otherwise English fallback.
5. Run the full backend suite.

### Task 7: Enforce coverage, deploy, and verify

**Objective:** Prevent untranslated static copy regressions and prove the feature works in the isolated test deployment.

**Files:**
- Create: `openresto-frontend/scripts/check-unlocalized-copy.*` (only if AST/lint coverage cannot enforce the rule)
- Modify: CI workflow(s) as required
- Update: `docs/plans/2026-07-22-bilingual-ui.md` with verification evidence

**Steps:**
1. Add a narrow CI check for newly introduced visible raw English literals outside explicitly allowed data/config files.
2. Run frontend unit, frontend E2E, format/lint, backend unit/integration, and Docker build suites.
3. Rebuild the isolated `test-rest` images from the committed fork and roll only that Compose project.
4. Verify `Accept-Language: es-CO` and `Accept-Language: en-US` defaults in a clean browser profile; verify explicit selector override survives reload.
5. Verify HTTPS, public booking flow, admin login, and a localized API error. Commit and push the implementation.
