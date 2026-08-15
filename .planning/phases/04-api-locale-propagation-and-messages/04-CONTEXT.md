# Phase 4: API Locale Propagation And Messages - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Centralize locale propagation from the frontend to the backend through shared API helpers, then localize representative backend API messages for auth, booking, hold, and admin flows. This phase covers product-owned request-language transport and user-facing `{ message }` values. It must preserve existing HTTP status codes, response body shapes, `message` keys, and tenant-authored restaurant/customer data exactly as they are today.

</domain>

<decisions>
## Implementation Decisions

### Locale transport
- **D-01:** Frontend API calls must set `Accept-Language` centrally from the active locale instead of per-screen headers.
- **D-02:** Direct internal API `fetch()` usage for uploads/deletes must reuse the same central locale/header path as JSON helpers.
- **D-03:** Existing locale persistence and detection stay owned by the frontend `I18nContext` and storage/browser-language helpers; Phase 4 reuses that contract rather than creating another locale source.

### Backend localization
- **D-04:** The backend must resolve request language centrally from `Accept-Language`, with English fallback.
- **D-05:** Localized backend responses must preserve existing HTTP status codes and existing response body shapes, especially the `{ message }` contract already used by the frontend and tests.
- **D-06:** Representative auth, booking, hold, and admin responses are in scope now; broader backend/email copy remains for later phases.

### Preservation and safety
- **D-07:** Restaurant-authored and customer-authored values remain verbatim; only product-owned copy surrounding them may localize.
- **D-08:** Controller/service flows that already encode business semantics in exception types or existing result objects should keep those semantics; localization should wrap the final user-facing text, not redesign control flow.
- **D-09:** Dynamic message values such as seat counts, email addresses, and exception details may be interpolated into localized templates, but the payload key names must not change.

### Verification
- **D-10:** Frontend verification must prove central `Accept-Language` propagation in shared helpers, including file-upload/delete paths that bypass JSON wrappers.
- **D-11:** Backend verification must prove representative `en` and `es-CO` responses for auth, booking, hold, and admin message paths while preserving status codes and `{ message }` bodies.

</decisions>

<specifics>
## Specific Ideas

- Frontend shared transport seam:
  - `openresto-frontend/api/client.ts`
- Frontend internal API callers that bypass the JSON helper today:
  - `openresto-frontend/api/admin.ts`
  - `openresto-frontend/api/restaurants.ts`
- Existing screen-level locale header hotspot previously identified in the repo:
  - `openresto-frontend/app/(user)/booking-confirmation/[bookingRef].tsx`
- Backend message/controller seams with representative Phase 4 scope:
  - `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`
  - `OpenRestoApi/Controllers/AuthController.cs`
  - `OpenRestoApi/Controllers/BookingsController.cs`
  - `OpenRestoApi/Controllers/HoldsController.cs`
  - `OpenRestoApi/Controllers/AdminController.cs`
- Backend service exceptions already carrying user-facing English messages:
  - `OpenRestoApi/Core/Application/Services/AuthService.cs`
  - `OpenRestoApi/Core/Application/Services/BookingService.cs`
  - `OpenRestoApi/Core/Application/Services/AdminService.cs`

</specifics>

<canonical_refs>
## Canonical References

### Roadmap and constraints
- `.planning/PROJECT.md` - overall localization objective and preservation rule.
- `.planning/REQUIREMENTS.md` - `PUB-03`, `API-01`, `API-02`, `API-03`.
- `.planning/ROADMAP.md` - approved Phase 4 goal and plan structure.
- `.planning/STATE.md` - current project status after Phase 3 closeout.
- `.planning/phases/01-baseline-and-contracts/01-01-SUMMARY.md` - shared transport/message seam audit.
- `.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md` - locale/preservation/HTTP contract rules.
- `.planning/phases/03-admin-ui-localization/03-VERIFICATION.md` - prior phase completion evidence and next-step transition.

### Implementation surfaces
- `openresto-frontend/context/I18nContext.tsx`
- `openresto-frontend/i18n/locale.ts`
- `openresto-frontend/services/storage.ts`
- `openresto-frontend/api/client.ts`
- `openresto-frontend/api/auth.ts`
- `openresto-frontend/api/holds.ts`
- `openresto-frontend/api/admin.ts`
- `openresto-frontend/api/restaurants.ts`
- `OpenRestoApi/Program.cs`
- `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`
- `OpenRestoApi/Controllers/AuthController.cs`
- `OpenRestoApi/Controllers/BookingsController.cs`
- `OpenRestoApi/Controllers/HoldsController.cs`
- `OpenRestoApi/Controllers/AdminController.cs`

### Backlog inputs
- `i18n/inventory/backend-api.json` - representative backend message backlog.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- The frontend already persists the active locale in storage and normalizes browser-language detection to `en` or `es-CO`.
- The shared API client already centralizes URL building, credentials, and JSON request bodies; Phase 4 can extend that seam instead of scattering locale headers.
- The backend already routes many user-facing failures through typed exceptions and `GlobalExceptionHandler`, which is the safest place to preserve status-code semantics while localizing message text.

### Established Patterns
- Frontend API modules consistently expect a stable `{ message }` response shape on user-visible failures.
- Backend controllers sometimes return anonymous `{ message = ... }` objects and sometimes `MessageResponse`; both serialize to the same `message` key and must stay that way.
- Representative business-rule/validation strings already live in services; localizing their final surfaced text centrally is safer than rewriting business logic branches.

### Integration Points
- Auth flows mix direct controller responses and service-thrown validation/business-rule exceptions.
- Booking and hold flows already rely on descriptive backend `message` values that the public/admin frontend surfaces directly.
- Admin flows include both success messages and user-facing error strings, including dynamic interpolation such as email recipients and seat counts.

</code_context>

<deferred>
## Deferred Ideas

- Full backend copy coverage beyond representative Phase 4 auth/booking/hold/admin paths.
- Email and outbound notification localization in generated content (Phase 5).
- Regression scanning for untranslated backend/user-visible strings (Phase 6).

</deferred>

---

*Phase: 04-api-locale-propagation-and-messages*
*Context gathered: 2026-07-23*
