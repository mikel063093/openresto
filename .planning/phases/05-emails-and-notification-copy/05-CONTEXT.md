# Phase 5: Emails And Notification Copy - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Localize outbound booking/admin email copy and notification-related product copy using request or booking language with safe English fallback. This phase covers transactional booking confirmation subjects/body copy, notification push payload copy, and notification-endpoint validation copy where the backend still owns user-facing English strings. It must preserve brand theming, tenant-authored restaurant/customer data, email settings contracts, notification payload/API shapes, and all existing endpoint/status semantics.

</domain>

<decisions>
## Implementation Decisions

### Locale source and fallback
- **D-01:** Phase 5 reuses the Phase 4 locale contract: request language resolves from `Accept-Language` with English fallback via the backend localization seam.
- **D-02:** Immediate booking confirmation emails should render from the current request language because confirmation sending happens synchronously off the booking request.
- **D-03:** Queued notification work items must carry the resolved locale captured at enqueue time so background push delivery can localize copy without depending on ambient request state.

### Email and notification scope
- **D-04:** Only product-owned email subject/body copy may localize; tenant-authored restaurant names, addresses, section/table names, special requests, admin-authored email subject/body content, and other stored business data remain verbatim.
- **D-05:** Existing brand theming, footer chrome, website links, and email settings behavior stay intact; localization can parameterize product-owned chrome but must not redesign the email shell.
- **D-06:** Notification-related backend copy in scope includes Web Push payload title/body text and notification-controller validation errors that the backend still returns directly.

### Contracts and preservation
- **D-07:** No persistence schema changes, request DTO changes, response body key changes, or notification payload shape changes are allowed.
- **D-08:** English remains the safe fallback for unsupported/absent locales, and `es`, `es_*`, or `es-*` variants normalize to `es-CO`.
- **D-09:** Date/time formatting inside localized emails and notification payload text should follow the resolved locale while still using the restaurant timezone for wall-clock values.

### Verification
- **D-10:** Focused backend tests must prove English and `es-CO` booking confirmation subjects/body rendering, including fallback behavior and preservation of tenant-authored content.
- **D-11:** Focused backend tests must prove localized notification push copy and notification-endpoint validation copy without changing payload/status contracts.

</decisions>

<specifics>
## Specific Ideas

- Booking confirmation email seams:
  - `OpenRestoApi/Core/Application/Services/BookingService.cs`
  - `OpenRestoApi/Core/Application/Services/BookingConfirmationService.cs`
  - `OpenRestoApi/Core/Application/Services/EmailTemplateService.cs`
  - `OpenRestoApi/Core/Application/Interfaces/IEmailTemplateService.cs`
  - `OpenRestoApi/Core/Application/Utilities/DateFormatter.cs`
- Admin/manual email wrapper seam:
  - `OpenRestoApi/Core/Application/Services/EmailHelper.cs`
- Notification localization seams:
  - `OpenRestoApi/Core/Application/Services/BookingNotificationService.cs`
  - `OpenRestoApi/Infrastructure/Notifications/NotificationWorkItem.cs`
  - `OpenRestoApi/Infrastructure/Notifications/NotificationQueue.cs`
  - `OpenRestoApi/Infrastructure/Notifications/NotificationWorker.cs`
  - `OpenRestoApi/Core/Application/Interfaces/INotificationQueue.cs`
  - `OpenRestoApi/Core/Application/Interfaces/IBookingNotificationService.cs`
  - `OpenRestoApi/Controllers/NotificationsController.cs`
- Existing localization/fallback seam to extend:
  - `OpenRestoApi/Infrastructure/Localization/ApiLocalization.cs`

</specifics>

<canonical_refs>
## Canonical References

### Roadmap and constraints
- `.planning/PROJECT.md` - overall localization objective and preservation rules.
- `.planning/REQUIREMENTS.md` - `MAIL-01`, `MAIL-02`.
- `.planning/ROADMAP.md` - approved Phase 5 goal and plan structure.
- `.planning/STATE.md` - current state says Phase 5 is intentionally not started.
- `.planning/phases/04-api-locale-propagation-and-messages/04-CONTEXT.md` - locale source and backend localization decisions already locked in Phase 4.
- `.planning/phases/04-api-locale-propagation-and-messages/04-VERIFICATION.md` - prior-phase evidence and transition boundary.

### Implementation surfaces
- `OpenRestoApi/Core/Application/Services/BookingConfirmationService.cs`
- `OpenRestoApi/Core/Application/Services/EmailTemplateService.cs`
- `OpenRestoApi/Core/Application/Services/EmailHelper.cs`
- `OpenRestoApi/Core/Application/Services/BookingNotificationService.cs`
- `OpenRestoApi/Controllers/NotificationsController.cs`
- `OpenRestoApi/Infrastructure/Localization/ApiLocalization.cs`
- `OpenRestoApi/Core/Application/Utilities/DateFormatter.cs`

### Existing automated evidence
- `OpenRestoApi.Tests/Services/BookingConfirmationServiceTests.cs`
- `OpenRestoApi.Tests/Services/EmailTemplateServiceTests.cs`
- `OpenRestoApi.Tests/Services/BookingNotificationServiceTests.cs`
- `OpenRestoApi.Tests/Services/NotificationServiceTests.cs`
- `OpenRestoApi.Tests/Controllers/NotificationsControllerUnitTests.cs`
- `OpenRestoApi.Tests/Utilities/DateFormatterTests.cs`
- `OpenRestoApi.Tests/Infrastructure/NotificationQueueTests.cs`
- `OpenRestoApi.Tests/Infrastructure/NotificationWorkerTests.cs`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ApiLocalization.ResolveLocale()` already normalizes request language to `en` or `es-CO` with English fallback.
- `BookingConfirmationService` already centralizes SMTP/settings/brand orchestration, so Phase 5 can localize email inputs there without disturbing booking persistence.
- `BookingNotificationService` already owns both notification-row persistence and push payload rendering, making it the safest place to localize notification copy while preserving payload structure.

### Established Patterns
- Confirmation emails already separate product-owned chrome from tenant-authored restaurant data through `EmailTemplateService` and `EmailTemplateBuilder`.
- Background notifications already flow through `INotificationQueue` work items into `NotificationWorker`; carrying locale on those work items preserves the existing async architecture.
- Notification controller validation errors currently return anonymous `{ error = ... }` bodies; that serialized key must remain unchanged if localized.

### Integration Points
- Public booking creation uses `BookingsController -> BookingService -> BookingConfirmationService` and enqueues notification work from the same request.
- Admin booking creation/cancellation uses `AdminController -> AdminService` and also enqueues notification work that should inherit the initiating request locale.
- Notification list rendering in the frontend already localizes type labels and timestamps client-side, so backend work here should stay limited to product-owned payload/error text it still owns directly.

</code_context>

<deferred>
## Deferred Ideas

- Broader backend copy regression sweeps beyond the explicit email/notification seams in this phase.
- Any additional locale persistence on bookings or customers; Phase 5 should rely on request-captured locale only.
- Phase 6 regression enforcement and final localization gatekeeping.

</deferred>

---

*Phase: 05-emails-and-notification-copy*
*Context gathered: 2026-07-23*
