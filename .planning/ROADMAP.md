# Roadmap: OpenResto Localization Baseline

## Overview

This roadmap completes OpenResto localization as a brownfield migration across the existing Expo Router frontend and ASP.NET Core backend. The work is sequenced to first lock the locale contract and inventory baseline, then finish public UI and admin UI coverage, then centralize client-to-server language propagation and backend messages, then localize emails/notifications, and finally close with enforcement and verification gates so localization does not regress.

## Phases

- [x] **Phase 1: Baseline And Contracts** - Freeze scope, classify inventory gaps, and define the locale/content-preservation contract from current code.
- [x] **Phase 2: Public UI Localization** - Complete public-facing product copy and locale-aware formatting without touching tenant-authored content.
- [x] **Phase 3: Admin UI Localization** - Complete admin product copy, role-safe labels, and locale-aware admin formatting.
- [x] **Phase 4: API Locale Propagation And Messages** - Centralize `Accept-Language` propagation and localize backend API responses while preserving contracts.
- [ ] **Phase 5: Emails And Notification Copy** - Localize outbound email and notification copy using request or booking language with safe fallback behavior.
- [ ] **Phase 6: Regression Gates And Final Verification** - Add durable detection for untranslated copy and prove both locales through automated and manual gates.

## Phase Details

### Phase 1: Baseline And Contracts
**Goal**: Convert the current inventories and partial i18n implementation into a phase-ready contract for frontend, backend, and preserved content boundaries.
**Depends on**: Nothing (first phase)
**Requirements**: LOC-01, QUAL-01
**Success Criteria** (what must be TRUE):
1. A canonical list of localization surfaces exists for public UI, admin UI, backend API, and emails based on current repo evidence.
2. The project explicitly distinguishes product-owned copy from tenant-authored restaurant content and documents what must never be translated.
3. The current locale-selection, formatting, and request-propagation seams are identified with target insertion points for later phases.
**Plans**: 2 plans

Plans:
- [x] 01-01: Reconcile inventories, prior plan inputs, and current partial i18n coverage into a current-state localization map.
- [x] 01-02: Author the preservation/contract rules and testing expectations for the remaining phases.

**Test Gate**:
- No code execution required for onboarding, but plan approval must cite the current inventory counts and the existing test suites that will enforce later phases.

### Phase 2: Public UI Localization
**Goal**: Finish public experience localization and locale-aware shared formatting across guest flows.
**Depends on**: Phase 1
**Requirements**: LOC-02, LOC-03, PUB-01, PUB-02
**Success Criteria** (what must be TRUE):
1. Public navigation, search, location browsing, restaurant detail, booking, confirmation, lookup, error, and modal product copy render correctly in `en` and `es-CO`.
2. Public date/time/count formatting follows the active locale consistently instead of `undefined` locale defaults.
3. Restaurant-authored names, descriptions, highlights, and other owner-managed content remain unchanged in both locales.
**Plans**: 3 plans

Plans:
- [x] 02-01: Normalize shared public formatting and page-title/accessibility helpers onto the active locale.
- [x] 02-02: Migrate guest routes and shared public components to catalog-driven product copy.
- [x] 02-03: Add or update focused public unit/E2E coverage in both locales.

**Test Gate**:
- `openresto-frontend` Jest tests for public routes/components and i18n helpers pass.
- Playwright smoke coverage confirms at least one guest booking path in English and Spanish.

### Phase 3: Admin UI Localization
**Goal**: Finish admin product copy localization and admin-specific formatting behavior.
**Depends on**: Phase 2
**Requirements**: ADM-01, ADM-02, ADM-03
**Success Criteria** (what must be TRUE):
1. Admin login, dashboard, bookings, locations, notifications, settings, and modal flows render product-owned copy correctly in `en` and `es-CO`.
2. Admin static role descriptions, validation copy, keyboard help, and accessibility labels are localized without changing role identifiers in logic or API payloads.
3. Admin dashboards, booking tables, and detail views use the active locale for visible date/time summaries.
**Plans**: 3 plans

Plans:
- [x] 03-01: Migrate admin routes and navigation/browser-title surfaces to catalog-driven copy.
- [x] 03-02: Migrate admin shared components, validation states, and keyboard/help surfaces.
- [x] 03-03: Expand admin Jest and Playwright coverage for both locales.

**Test Gate**:
- Admin Jest suites pass for translated routes and components.
- Playwright admin smoke verifies login plus at least one booking-management path in both locales.

### Phase 4: API Locale Propagation And Messages
**Goal**: Route locale centrally from frontend clients to the backend and localize API-facing user messages without breaking HTTP/JSON contracts.
**Depends on**: Phase 3
**Requirements**: PUB-03, API-01, API-02, API-03
**Success Criteria** (what must be TRUE):
1. Frontend API clients set `Accept-Language` centrally from the active locale rather than screen-specific headers.
2. The backend resolves request language centrally and serves localized user-facing messages with English fallback.
3. Existing response shapes, `message` keys, and status-code semantics remain unchanged across localized endpoints.
**Plans**: 3 plans

Plans:
- [x] 04-01: Centralize locale propagation in frontend API helpers and remove ad hoc header handling.
- [x] 04-02: Introduce backend localization infrastructure and migrate high-value controller/service message paths.
- [x] 04-03: Add/update backend unit and integration tests for representative `en` and `es-CO` API responses.

**Test Gate**:
- Frontend API tests assert locale propagation.
- Backend unit/integration tests cover representative auth, booking, hold, and admin error/success messages in both locales.

### Phase 5: Emails And Notification Copy
**Goal**: Localize outbound communication generated by the backend while preserving branding and authored content.
**Depends on**: Phase 4
**Requirements**: MAIL-01, MAIL-02
**Success Criteria** (what must be TRUE):
1. Transactional emails select localized product-owned copy and subjects using request or booking language with English fallback.
2. Email templates preserve current brand theming and do not translate tenant-authored restaurant data inserted into the message.
3. Notification-related product copy follows the same locale rules as API messages where applicable.
**Plans**: 2 plans

Plans:
- [ ] 05-01: Localize email subject/body generation paths and document language fallback behavior.
- [ ] 05-02: Add/update backend service tests for localized email rendering and notification copy.

**Test Gate**:
- Backend email/template tests pass for English and Spanish subject/body output.
- No contract changes to email settings or API endpoints are introduced.

### Phase 6: Regression Gates And Final Verification
**Goal**: Prevent untranslated regressions and verify the full localization objective end-to-end.
**Depends on**: Phase 5
**Requirements**: QUAL-02, QUAL-03
**Success Criteria** (what must be TRUE):
1. Automated checks exist for newly introduced untranslated visible copy or backend user messages outside approved allowlists.
2. The combined frontend and backend test matrix demonstrates stable behavior for `en` and `es-CO`.
3. Final verification explicitly confirms contract preservation, authored-content preservation, locale-aware formatting, and end-to-end language propagation.
**Plans**: 3 plans

Plans:
- [ ] 06-01: Add narrow regression detection for untranslated strings and backend message gaps.
- [ ] 06-02: Run/record full automated verification coverage across frontend and backend localization surfaces.
- [ ] 06-03: Complete manual/UAT checks for the highest-risk bilingual guest/admin flows and preservation constraints.

**Test Gate**:
- Frontend Jest, frontend Playwright, backend unit, backend integration, and localization-specific checks all pass.
- Manual verification confirms `Accept-Language: es-CO` and `Accept-Language: en-US` behavior plus explicit override persistence.

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Baseline And Contracts | 2/2 | Complete | 2026-07-23 |
| 2. Public UI Localization | 3/3 | Complete | 2026-07-23 |
| 3. Admin UI Localization | 3/3 | Complete | 2026-07-23 |
| 4. API Locale Propagation And Messages | 3/3 | Complete | 2026-07-23 |
| 5. Emails And Notification Copy | 0/2 | Not started | - |
| 6. Regression Gates And Final Verification | 0/3 | Not started | - |
