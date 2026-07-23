# OpenResto Localization Baseline

## What This Is

OpenResto is a self-hosted restaurant booking platform with a public booking experience, an admin dashboard, and an ASP.NET Core API backing a shared Expo Router frontend. This planning baseline scopes a brownfield localization effort to complete and harden English and Colombian Spanish (`en`, `es-CO`) behavior across the public UI, admin UI, locale-aware formatting, request-language propagation, backend API messages, and outbound emails without changing authored restaurant content or API/HTTP contracts.

## Core Value

Users and restaurant staff should receive clear, correctly localized product behavior in English or Colombian Spanish without breaking booking flows, admin operations, or existing contracts.

## Business Context

- **Customer**: Independent restaurant operators and their guests using a self-hosted booking stack.
- **Revenue model**: Open-source self-hosted product; value is adoption, trust, and low-friction operations rather than SaaS lock-in.
- **Success metric**: A guest or admin can complete the same core workflows in `en` and `es-CO` with no untranslated product copy and no contract regressions.
- **Strategy notes**: Existing localization inventories in `i18n/inventory/` and the prior plan in `docs/plans/2026-07-22-bilingual-ui.md` are treated as planning inputs.

## Requirements

### Validated

- ✓ Guests can browse restaurants, search, place holds, book, and look up reservations across the public UI.
- ✓ Admins can authenticate and manage bookings, locations, notifications, and settings from the admin UI.
- ✓ Backend JSON responses already use stable `message` fields and established status-code behavior that callers and tests depend on.
- ✓ A partial frontend i18n layer already exists (`openresto-frontend/context/I18nContext.tsx`, `openresto-frontend/i18n/*`, `components/layout/LanguageSelector.tsx`) but coverage is incomplete.

### Active

- [ ] Complete `en` and `es-CO` coverage for public UI strings, titles, labels, accessibility copy, and user-visible empty/error states.
- [ ] Complete `en` and `es-CO` coverage for admin UI strings, titles, labels, validation copy, keyboard help, and role-sensitive static text.
- [ ] Normalize locale-aware formatting for dates, times, counts, and browser/document metadata using the active locale instead of ambient runtime defaults.
- [ ] Propagate request language centrally from the frontend to the API using `Accept-Language`, then localize backend API messages and email/notification content while preserving existing JSON keys and status codes.
- [ ] Preserve tenant-authored restaurant content exactly as authored; do not machine-translate names, descriptions, highlights, footer copy, social labels, or other owner-managed text.
- [ ] Add regression gates so new visible raw strings and untranslated backend messages are caught before execution closes a phase.

### Out of Scope

- Automatic translation of restaurant-authored content — excluded because it changes user data semantics and violates the preservation constraint.
- New locales beyond English and Colombian Spanish — excluded to keep the phase bounded and aligned with the existing inventories.
- API shape redesign, status-code changes, or replacing `message` payload conventions — excluded because callers and tests rely on current contracts.
- Product deployment, release operations, or dependency upgrades — excluded because this onboarding request is planning only.

## Context

The repository is a brownfield monorepo with an ASP.NET Core 10 backend (`OpenRestoApi`), an Expo Router frontend (`openresto-frontend`) targeting web and mobile, repo-level Docker/Nginx infrastructure, and broad automated test coverage. Existing i18n inventories enumerate 79 public UI entries, 209 admin UI entries, and 93 backend API/email entries needing review. The frontend already contains a lightweight locale provider and a small bilingual catalog, but many route and component surfaces still format with `toLocaleDateString(undefined, ...)` or contain raw English strings. The backend centralizes exception translation through `GlobalExceptionHandler`, but user-facing messages remain hard-coded across controllers and email flows. A prior bilingual UI plan exists in `docs/plans/2026-07-22-bilingual-ui.md`; this roadmap expands that effort to complete backend and email localization and to preserve tenant-authored content and existing contracts.

## Constraints

- **Tech stack**: Must fit the existing Expo Router frontend and ASP.NET Core backend — no planning assumption should require replacing either stack.
- **Contract stability**: Preserve HTTP status codes, JSON keys, and `MessageResponse` usage — tests and callers already depend on them.
- **Content preservation**: Restaurant-authored data must remain exactly authored — only product-owned copy can be localized.
- **Locale scope**: Deliver only `en` and `es-CO` — broader locale strategy is deferred.
- **Brownfield safety**: Use existing inventories and tests as the migration baseline — avoid speculative rewrites.
- **Planning-only request**: Create `.planning` artifacts only — no product-source changes, installs, commits, pushes, or deploys in this onboarding pass.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use existing inventories as the localization backlog seed | They already identify public, admin, and backend message surfaces and reduce missed scope | ✓ Good |
| Preserve authored restaurant content verbatim | User constraint and product correctness require separating product copy from tenant data | ✓ Good |
| Keep `message` JSON bodies and status codes stable | Frontend code and tests read `body.message` and expect current HTTP semantics | ✓ Good |
| Plan frontend and backend localization as phased brownfield work instead of a single sweep | Existing partial i18n implementation and wide test surface favor incremental verification | ✓ Good |
| Disable GSD research for this baseline | The repository already contains enough local evidence and the user explicitly requested research disabled | ✓ Good |

---
*Last updated: 2026-07-23 after brownfield onboarding baseline creation*
