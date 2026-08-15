# Structure

**Analysis Date:** 2026-07-23

## Top-Level Layout

- `OpenRestoApi/` - ASP.NET Core API source
- `OpenRestoApi.Tests/` - backend unit, integration, migration, and infrastructure tests
- `openresto-frontend/` - Expo Router frontend source and tests
- `i18n/inventory/` - localization inventory manifests and per-surface extraction outputs
- `docs/` - existing planning/support docs, including bilingual UI and RBAC plans
- `nginx/`, `docker-compose.e2e.yml`, `traefik/` - infrastructure/runtime support

## Backend Layout

- `OpenRestoApi/Controllers/` - HTTP entry points and response shaping
- `OpenRestoApi/Core/Application/` - DTOs, interfaces, services, mappings, utilities
- `OpenRestoApi/Core/Domain/` - core business entities
- `OpenRestoApi/Infrastructure/` - auth, cookies, email, exceptions, holds, notifications, persistence
- `OpenRestoApi/Extensions/` - service and startup wiring
- `OpenRestoApi/Migrations/` - EF Core schema history

## Frontend Layout

- `openresto-frontend/app/(user)/` - public guest routes
- `openresto-frontend/app/admin/` - admin routes
- `openresto-frontend/components/booking/` - booking UI building blocks
- `openresto-frontend/components/admin/` - admin UI building blocks
- `openresto-frontend/components/layout/` - shared nav/layout surfaces, including language selector
- `openresto-frontend/context/` - provider state such as brand/theme/i18n
- `openresto-frontend/i18n/` - locale detection and message catalogs
- `openresto-frontend/api/` - shared HTTP client and endpoint wrappers
- `openresto-frontend/utils/` - formatting and shared helpers
- `openresto-frontend/tests/` - frontend unit/component tests
- `openresto-frontend/e2e/` - Playwright end-to-end tests

## Planning Inputs Relevant To Localization

- `docs/plans/2026-07-22-bilingual-ui.md` - prior bilingual UI plan
- `i18n/inventory/public-ui.json` - 79 public UI inventory entries
- `i18n/inventory/admin-ui.json` - 209 admin UI inventory entries
- `i18n/inventory/backend-api.json` - 93 backend API/email inventory entries
- `openresto-frontend/context/I18nContext.tsx` and `i18n/messages.ts` - partial implementation already in repo

## High-Risk Structure Notes

- Public/admin routes and shared components are split across many files, so localization will touch broad but related UI slices.
- Backend user messages are distributed across controllers, services, and email builders, so a central catalog seam is required to avoid fragmented translation logic.
- Existing tests are already organized by route/component/service, which is favorable for incremental localization plans.

---
*Structure analysis: 2026-07-23*
*Update when major directory patterns change*
