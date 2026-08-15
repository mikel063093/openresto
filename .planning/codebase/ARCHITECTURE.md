# Architecture

**Analysis Date:** 2026-07-23

## Pattern Overview

**Overall:** Brownfield full-stack monorepo with a shared frontend, a monolithic HTTP API, and repo-level containerized infrastructure.

**Key Characteristics:**
- Single ASP.NET Core API process with layered services, repositories, and infrastructure
- Single Expo Router frontend serving both public and admin experiences from one codebase
- Shared test-heavy development model with unit, integration, and E2E coverage
- Partial localization already introduced on the frontend, but not yet propagated across all UI and backend seams

## Layers

**Frontend Route Layer:**
- Purpose: Define public and admin screens, page titles, route-specific data loading, and UI state
- Contains: `openresto-frontend/app/(user)/**`, `openresto-frontend/app/admin/**`
- Depends on: Context providers, API layer, shared components
- Used by: Browser/mobile runtime

**Frontend Shared UI Layer:**
- Purpose: Reusable booking, restaurant, admin, layout, and common component logic
- Contains: `openresto-frontend/components/**`, `hooks/**`, `utils/**`, `context/**`, `i18n/**`
- Depends on: React/Expo platform APIs and the frontend API layer
- Used by: Route layer

**API Boundary Layer:**
- Purpose: Route matching, auth, validation, status-code handling, and JSON response shaping
- Contains: `OpenRestoApi/Program.cs`, `Controllers/**`, exception handling middleware
- Depends on: Application services and infrastructure
- Used by: Frontend HTTP clients and integration tests

**Application/Domain Layer:**
- Purpose: Booking, admin, branding, notifications, and other business logic
- Contains: `OpenRestoApi/Core/Application/**`, `Core/Domain/**`
- Depends on: Interfaces, repositories, infrastructure abstractions
- Used by: Controllers and infrastructure workers

**Infrastructure Layer:**
- Purpose: Persistence, authentication, holds, email, notifications, and operational extensions
- Contains: `OpenRestoApi/Infrastructure/**`, `OpenRestoApi/Extensions/**`
- Depends on: Framework services and external libraries
- Used by: Application layer and startup wiring

## Data Flow

**HTTP Request / UI Interaction:**

1. User interacts with a public or admin screen in Expo Router.
2. Screen/component calls a helper in `openresto-frontend/api/*`.
3. Frontend request goes to ASP.NET Core controllers through `api/client.ts`.
4. Controller validates/authenticates and dispatches to application services.
5. Services read/write persistence or invoke infrastructure (holds, email, notifications).
6. Response returns to the frontend as JSON, often with a `message` payload for user-visible state.
7. UI renders a mix of product-owned copy and tenant-authored restaurant content.

**State Management:**
- Frontend state is component/context-driven; locale and brand are provider-based.
- Backend request handling is stateless except for persistence, cookies, and hold state.
- Holds use dedicated infrastructure and time-sensitive policies.

## Key Abstractions

**Locale Context:**
- Purpose: Resolve and expose the active frontend locale plus `t()` behavior
- Examples: `I18nProvider`, `useI18n`, `translate()`
- Pattern: Context provider + typed catalog helpers

**Message Contract:**
- Purpose: Standardize user-visible API messages
- Examples: `MessageResponse`, `GlobalExceptionHandler`, hard-coded controller messages
- Pattern: JSON DTO + centralized exception translation

**Service Layer:**
- Purpose: Keep business logic out of controllers
- Examples: booking, admin, auth, brand, notification, email-related services
- Pattern: DI-registered application services

## Entry Points

**Frontend App Root:**
- Location: `openresto-frontend/app/_layout.tsx`
- Triggers: Browser/mobile app boot
- Responsibilities: Mount providers, theme, brand, and locale behavior

**Backend App Root:**
- Location: `OpenRestoApi/Program.cs`
- Triggers: Process startup and each HTTP request
- Responsibilities: Configure middleware, auth, rate limiting, exception handling, controllers, database initialization

## Error Handling

**Strategy:** Backend exceptions bubble into a global handler that preserves the established `{ message }` response body shape for typed domain exceptions.

**Patterns:**
- Controllers also return explicit `MessageResponse` bodies for many expected outcomes
- Frontend tests and code frequently read `body.message`
- Localization must change text, not shape or status semantics

## Cross-Cutting Concerns

**Validation:**
- Mixed controller/service validation with test coverage on many edge cases

**Authentication:**
- JWT bearer + cookies for admin/session behavior

**Localization:**
- Partial frontend catalog/provider exists
- Backend has no complete central localization layer yet
- Inventories in `i18n/inventory/` identify current untranslated surfaces

---
*Architecture analysis: 2026-07-23*
*Update when major patterns change*
