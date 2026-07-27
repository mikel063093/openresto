# Codebase Map

## Repo Shape
- `OpenRestoApi/`: ASP.NET Core 10 backend, EF Core + SQLite, controllers/services/repositories split.
- `openresto-frontend/`: Expo Router frontend for web/mobile.
- `OpenRestoApi.Tests/`: xUnit unit, controller, migration, service, and integration coverage.
- `nginx/`, `nginx-vps/`, `docker-compose*.yml`: local, release, test, and VPS deploy topology.
- `docs/plans/`: prior implementation plans already present in-repo.

## Evidence-Backed Current State

### Backend entrypoints and middleware
- `OpenRestoApi/Program.cs`: registers ProblemDetails, exception handler, CORS, rate limiting, JWT auth, EF database setup, and startup `InitializeDatabase(...)`.
- `OpenRestoApi/Extensions/ServiceCollectionExtensions.cs`: configures JWT bearer auth, cookie token fallback, role policies (`SuperAdminOnly`, `BookingsRead`, `BookingsWrite`), in-memory holds, repositories, services, and ASP.NET rate-limit policies.
- `OpenRestoApi/Extensions/DatabaseExtensions.cs`: runs `Database.Migrate()` on startup, seeds bootstrap admin credentials when needed, and contains SQLite-specific migration remap/repair logic.

### Booking and availability flow
- `OpenRestoApi/Controllers/AvailabilityController.cs`: public availability lookup at `GET /api/restaurants/{restaurantId}/availability`.
- `OpenRestoApi/Controllers/HoldsController.cs`: public temporary holds with optional auto-assign and explicit release.
- `OpenRestoApi/Controllers/BookingsController.cs`: public create, lookup-by-reference+email, cancel-by-reference+email, recent-bookings cookie, plus authenticated CRUD endpoints.
- `OpenRestoApi/Core/Application/Services/AvailabilityService.cs`: computes slots from restaurant hours, timezone, bookings, and in-memory holds.
- `OpenRestoApi/Core/Application/Services/BookingService.cs`: validates pause state, local-to-UTC normalization, walk-in rules, hold consumption, overlap checks, table capacity, and booking persistence.
- `OpenRestoApi/Core/Application/DTOs/BookingDto.cs`: current public booking contract includes `RestaurantId`, `Date`, `CustomerEmail`, `CustomerName`, `Seats`, `HoldId`, `TableId`, `SectionId`, cancellation fields, and display fields.

### Admin auth and authorization
- `OpenRestoApi/Controllers/AuthController.cs`: admin login/logout/session/password/email/PVQ endpoints.
- `OpenRestoApi/Core/Application/Services/AuthService.cs`: authenticates `AdminCredential`, supports bootstrap admin creation, and issues JWTs.
- `OpenRestoApi/Core/Application/Services/JwtTokenService.cs`: current JWTs carry only `ClaimTypes.Email` and `ClaimTypes.Role`, with a 30-day expiry.
- `OpenRestoApi/Core/Domain/AdminCredential.cs`: admin users currently store `Email`, password hash/salt, `Role`, `IsActive`, PVQ/reset fields.
- `OpenRestoApi/Core/Domain/AdminRole.cs`: current roles are `SuperAdmin`, `BookingViewer`, `BookingEditor`.
- `OpenRestoApi/Controllers/AdminController.cs`: current reservation management surface is admin-only and policy-gated by broad booking read/write roles rather than per-record ownership.

### Persistence and migration conventions
- `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`: EF model with `Restaurants`, `Sections`, `Tables`, `Bookings`, `AdminCredentials`, notifications, branding, social links, and email tables.
- `OpenRestoApi/Core/Domain/Booking.cs`: booking entity currently has no creator identity column; ownership today is based on public lookup by `BookingRef + CustomerEmail` or broad admin permissions.
- `OpenRestoApi/Migrations/`: timestamp-prefixed EF Core migrations, paired `.Designer.cs` files, and `AppDbContextModelSnapshot.cs`.
- Existing examples: `20260722011455_AddAdminCredentialRoles.cs`, `20260722013508_AddAdminCredentialIsActive.cs`, `20260719220106_AddBookingSlotIntervalMinutes.cs`.

### Deploy and hosting
- `docker-compose.vps.yml`: VPS topology with backend, frontend, and `reverse-proxy` services; backend listens on `8080`, frontend on `8081`, TLS terminates in Nginx.
- `nginx-vps/default.conf.template`: redirects HTTP to HTTPS and proxies `/api/` to backend with request rate limiting and security headers.
- `README.md` and `nginx-vps/README.md`: document self-hosted Docker/VPS deployment and required env vars.

### Tests and API documentation
- Integration coverage exists for auth, bookings, availability, role authorization, admin users, holds, restaurants, and other flows under `OpenRestoApi.Tests/Integration/`.
- Unit coverage exists for hold service, availability service, auth/jwt services, migrations, and controller/service behaviors.
- Frontend E2E coverage exists under `openresto-frontend/e2e/`.
- OpenAPI is exposed in development/testing via `app.MapOpenApi()` in `Program.cs`; there is no checked-in curated MCP-specific API document yet.

## Existing Constraints Relevant To `internal-operator-mcp`
- Single-instance assumptions exist today for holds: `OpenRestoApi/Infrastructure/Holds/HoldService.cs` is in-memory singleton and explicitly notes Redis would be needed for multi-instance scale.
- Current admin JWTs are too broad and too long-lived for agent access; they do not encode restaurant scope or operator identity.
- Reservation ownership required by the new feature is not yet representable in the `Booking` entity.
- Current VPS topology already supports remote HTTPS access without introducing a second edge proxy stack.

## Existing Planning Inputs
- `docs/plans/2026-07-22-rbac-booking-roles.md`: recent role-based access design already aligned to `AdminCredential` and booking policies.
- `docs/plans/2026-07-22-bilingual-ui.md`: recent evidence that repo uses plan documents for decision-making and testing-first execution.
