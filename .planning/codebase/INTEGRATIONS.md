# Integrations

**Analysis Date:** 2026-07-23

## External Interfaces

### Frontend -> Backend HTTP
- `openresto-frontend/api/*.ts` centralizes API access through `api/client.ts`.
- Existing callers depend on stable JSON shapes, especially `{ message: string }` error/success bodies.
- Localization work needs a single `Accept-Language` propagation path here instead of per-screen headers.

### Backend -> SMTP
- `OpenRestoApi/Infrastructure/Email/EmailService.cs` sends transactional mail through SMTP settings stored in the database.
- Email localization must preserve current transport behavior and brand rendering.

### Browser / Device Locale
- `openresto-frontend/i18n/locale.ts` and `context/I18nContext.tsx` read stored locale plus browser/device languages.
- Several UI surfaces still bypass the active locale and call `toLocaleDateString(undefined, ...)` directly.

### Database / Persisted Content
- Restaurant names, descriptions, highlights, footer settings, social links, and other tenant data live in the database and flow into the UI/API as authored content.
- These values are a preservation boundary, not translation targets.

## Internal Contracts That Behave Like Integrations

- `MessageResponse` in `OpenRestoApi/Core/Application/DTOs/AdminDto.cs` anchors the backend message contract.
- `GlobalExceptionHandler` maps domain exceptions into JSON bodies with current message semantics.
- Existing inventories in `i18n/inventory/*.json` function as planning input artifacts for localization coverage.

## Testing/Verification Surfaces

- Frontend API tests under `openresto-frontend/tests/api`
- Backend integration tests under `OpenRestoApi.Tests/Integration`
- Playwright E2E flows across booking/admin journeys

## Localization Implications

- Add locale propagation at the shared frontend client layer, not ad hoc route code.
- Add backend request-culture resolution in a central middleware/service seam.
- Preserve transport contracts and tenant-authored data boundaries across all integrations.

---
*Integration analysis: 2026-07-23*
*Update when major integration points change*
