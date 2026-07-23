# Phase 4: API Locale Propagation And Messages - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Route the active frontend locale through shared API transport and localize backend API-facing user messages without changing existing response shapes, `message` keys, machine-readable fields, or HTTP status semantics. This phase covers shared frontend client headers, backend request-locale resolution, and representative auth/booking/hold/admin/validation message paths. It does not localize outbound email/template copy or tenant-authored restaurant content.

</domain>

<decisions>
## Implementation Decisions

### Locale propagation
- **D-01:** Frontend API requests must derive `Accept-Language` centrally from the active `en` or `es-CO` locale rather than from per-screen headers.
- **D-02:** Backend request locale must resolve centrally from `Accept-Language` with English fallback when the header is absent, invalid, or outside the supported `en` / `es-CO` scope.
- **D-03:** Existing explicit user locale choice remains the source of truth on the frontend; API transport should read the same persisted locale contract already established in Phase 2.

### Backend message localization
- **D-04:** User-facing API messages in auth, booking, hold, admin, and representative validation flows should move behind a shared backend localization seam rather than scattering `Accept-Language` parsing across controllers and services.
- **D-05:** Localized messages must preserve the current JSON body shape, especially the lowercase `message` field consumed by frontend code, tests, and E2E flows.
- **D-06:** Tenant-authored data inserted into localized responses, such as customer email addresses or restaurant names, must remain verbatim.

### Verification
- **D-07:** Phase 4 verification requires focused frontend API tests proving centralized locale propagation and focused backend unit/integration coverage proving representative English and `es-CO` message behavior.
- **D-08:** Existing HTTP status codes, non-message fields, and success/error flow semantics are contract-sensitive and must remain unchanged while messages localize.
- **D-09:** Email/notification-copy localization remains deferred to Phase 5 even when those code paths reuse backend localization helpers later.

### the agent's Discretion
- Exact backend service/class names, message-key naming, and the split between controller-owned versus service-thrown localized strings can be chosen as long as locale resolution stays centralized and the response contracts above remain intact.

</decisions>

<specifics>
## Specific Ideas

- Frontend transport seams:
  - `openresto-frontend/api/client.ts`
  - `openresto-frontend/services/storage.ts`
  - `openresto-frontend/i18n/locale.ts`
  - `openresto-frontend/app/(user)/booking-confirmation/[bookingRef].tsx`
- Frontend tests already covering shared API helpers:
  - `openresto-frontend/tests/api/client.test.ts`
  - `openresto-frontend/tests/api/auth.test.ts`
  - `openresto-frontend/tests/api/bookings.test.ts`
  - `openresto-frontend/tests/api/holds.test.ts`
- Backend message hotspots already visible in code:
  - `OpenRestoApi/Controllers/AuthController.cs`
  - `OpenRestoApi/Controllers/BookingsController.cs`
  - `OpenRestoApi/Controllers/HoldsController.cs`
  - `OpenRestoApi/Controllers/AdminController.cs`
  - `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`
  - `OpenRestoApi/Core/Application/Services/AuthService.cs`
  - `OpenRestoApi/Core/Application/Services/BookingService.cs`
  - `OpenRestoApi/Core/Application/Services/AdminService.cs`
- Backend tests already exercising these routes and exception mappings:
  - `OpenRestoApi.Tests/Infrastructure/GlobalExceptionHandlerTests.cs`
  - `OpenRestoApi.Tests/Integration/AuthControllerTests.cs`
  - `OpenRestoApi.Tests/Integration/BookingsControllerTests.cs`
  - `OpenRestoApi.Tests/Integration/HoldsControllerTests.cs`
  - `OpenRestoApi.Tests/Integration/AdminControllerTests.cs`

</specifics>

<canonical_refs>
## Canonical References

### Roadmap and constraints
- `.planning/PROJECT.md` — preserved-content, locale-scope, and contract constraints.
- `.planning/REQUIREMENTS.md` — `PUB-03`, `API-01`, `API-02`, `API-03`.
- `.planning/ROADMAP.md` — approved Phase 4 goal and plan structure.
- `.planning/STATE.md` — current project status after Phase 3.
- `.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md` — preservation and HTTP/JSON contract rules.
- `.planning/phases/02-public-ui-localization/02-01-SUMMARY.md` — established `en` / `es-CO` frontend locale contract.
- `.planning/phases/03-admin-ui-localization/03-VERIFICATION.md` — most recent verification style and stack evidence.

### Implementation surfaces
- `openresto-frontend/api/*.ts` and `openresto-frontend/api/client.ts` — shared transport and consumer APIs.
- `openresto-frontend/context/I18nContext.tsx`, `openresto-frontend/i18n/locale.ts`, `openresto-frontend/services/storage.ts` — active locale state and persistence.
- `OpenRestoApi/Program.cs` and `OpenRestoApi/Extensions/ServiceCollectionExtensions.cs` — backend pipeline and DI seams.
- `OpenRestoApi/Controllers/**` — direct API response-message paths.
- `OpenRestoApi/Core/Application/Services/**` — exception/message paths shared across controllers.
- `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs` — centralized `{ message }` exception mapping.

### Backlog inputs
- `i18n/inventory/backend-api.json` — backend untranslated/review-needed backlog.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Frontend locale persistence already lives in `StorageService` and `detectLocale()`, so the shared API client can resolve the active locale without introducing a new provider dependency.
- Backend exception mapping is already centralized in `GlobalExceptionHandler`, making it the right seam for localized thrown-message responses.
- Integration tests already assert `message` bodies for several representative endpoints, so Phase 4 can extend that coverage instead of inventing new harnesses.

### Established Patterns
- Frontend API helpers all route through `openresto-frontend/api/client.ts`, but one route-level screen still sends an ad hoc `Accept-Language` header directly.
- Backend controllers commonly return anonymous `{ message = "..." }` or `MessageResponse` values for success and expected rejection paths, while services throw typed domain exceptions for other user-visible failures.
- The backend does not currently use `IStringLocalizer` or ASP.NET request-localization middleware, so this phase is introducing a project-local seam rather than extending an existing localization framework.

### Integration Points
- `AuthController`, `BookingsController`, `HoldsController`, and `AdminController` collectively cover the representative auth/booking/hold/admin response patterns the roadmap calls out.
- `AuthService`, `BookingService`, and `AdminService` own many of the domain exception messages that flow through `GlobalExceptionHandler`.
- Backend integration tests use `TestWebAppFactory.CreateClient()` and can inject `Accept-Language` directly on requests without changing the broader test harness contract.

</code_context>

<deferred>
## Deferred Ideas

- Outbound email/template copy localization belongs to Phase 5.
- Broader backend sweep across every remaining controller/service message can continue in Phase 5/6 after the central locale seam is proven here.
- Final bilingual E2E/UAT closure and regression detection belong to Phase 6.

</deferred>

---

*Phase: 04-api-locale-propagation-and-messages*
*Context gathered: 2026-07-23*
