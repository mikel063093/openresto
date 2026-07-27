# internal-operator-mcp

## Status
Decision-ready planning draft for a Level-C feature. This document is based on verified repository evidence plus explicit design recommendations. Any item marked "proposed" is not implemented today.

## Objective
Add a remote HTTPS MCP surface for internal operators that can look up availability and manage only the reservations they created, using short-lived scoped credentials, server-enforced ownership, and VPS-compatible deployment.

## Current-State Evidence

### Verified today
- Public reservation flow already exists through availability, holds, booking creation, lookup, and cancellation endpoints in `AvailabilityController`, `HoldsController`, and `BookingsController`.
- Internal reservation management already exists through `AdminController`, protected by role policies `BookingsRead`, `BookingsWrite`, and `SuperAdminOnly`.
- Admin auth uses `AdminCredential` records and JWT bearer authentication, with cookie fallback for the existing admin UI.
- JWTs currently carry `email` and `role` claims only, and `JwtTokenService.Generate(...)` sets a 30-day expiry.
- `Booking` does not currently record an internal operator creator identity.
- EF migrations are timestamp-prefixed C# migrations with snapshot updates and backend migration tests.
- VPS deployment already terminates HTTPS in Nginx and proxies `/api/` to the ASP.NET backend.
- Server-side and edge rate limiting already exist, but they are IP-oriented and not designed for scoped agent credentials.

### Not verified and therefore not assumed
- No verified MCP server implementation exists in the repo.
- No verified machine-to-machine credential store exists beyond `AdminCredential`.
- No verified distributed cache or Redis deployment exists.
- No verified audit trail model exists for operator reservation actions beyond current booking fields and notifications.

## Scope

### In scope for MVP
- Operator-scoped remote MCP transport over HTTPS.
- Short-lived scoped agent credentials for one or more restaurants.
- Identity propagation that uniquely identifies the human/internal operator behind each MCP action.
- Reservation ownership persistence sufficient to enforce "own bookings only."
- Server-side authorization matrix for availability, create, list/get own, modify own, cancel own, and escalation.
- Minimal internal API contract supporting the MCP server.
- Audit, privacy, observability, and rate-limiting additions needed for safe internal use.
- Test plan and phased implementation plan.

### Out of scope for MVP
- Customer-facing MCP or WhatsApp integrations.
- Replacing public booking lookup/cancel flows.
- Multi-instance hold coordination or horizontal scaling redesign.
- Rich conversational escalation workflows beyond a concrete hand-off event/API.
- Broad admin UI redesign.
- New external infrastructure dependencies unless a later decision explicitly approves them.

## Required Invariants
- Every reservation created through the MCP must have a single, durable creator operator identity.
- A reservation created by operator `A` must never be readable, mutable, or cancellable through MCP credentials representing operator `B`.
- Restaurant scope is enforced server-side on every MCP request.
- Expired or revoked credentials must fail closed.
- MCP authorization must not depend on UI hiding or model behavior; backend checks are the security boundary.
- Public and admin reservation flows must continue to behave as they do today unless explicitly changed in implementation.
- All new timestamps remain UTC in persistence, matching existing EF conversion conventions.

## Identity Propagation Decision

### Decision
Use two identities per request:
- `credential_subject`: the short-lived agent credential or token instance presented to the MCP/API.
- `operator_id`: the durable internal operator identity on whose behalf the credential was minted.

Persist `operator_id` on reservations created via the MCP and authorize reservation access using `operator_id`, not the presented credential alone.

### Why
Using only a shared agent token would make ownership ambiguous. A shared token can rotate, be reused across tools, or represent multiple operators over time. The requirement says "own bookings" belong to the internal operator identity that created them, so ownership must be tied to a durable operator principal, not to a transient credential string.

### Consequence
The credential must carry or resolve:
- operator identity
- restaurant scope
- expiry
- revocation state
- optional token id / jti for audit and revocation

## Recommended Data Model And Migration Plan

### Proposed new persistence concepts
- `OperatorPrincipal`
  - durable internal operator identity
  - normalized human-readable identifier such as email or internal handle
  - active flag
- `OperatorRestaurantScope`
  - join table between operator principal and restaurant ids
  - supports one-to-many restaurant scope
- `OperatorAgentCredential`
  - stores credential metadata only, not plaintext bearer tokens
  - fields: id, operator principal id, credential key id / jti, hashed secret or token digest, issued-at, expires-at, revoked-at, last-used-at, allowed restaurant scope snapshot if needed, optional notes
- booking ownership fields on `Booking`
  - `CreatedByOperatorId` nullable at first for backward compatibility
  - `CreatedViaChannel` or equivalent discriminator if needed to separate MCP-created bookings from existing public/admin bookings

### Migration strategy
1. Add new operator and credential tables plus nullable booking ownership fields in one additive migration.
2. Leave existing bookings null-owned; only MCP-created bookings require ownership enforcement.
3. Add indexes for:
   - normalized operator identity uniqueness
   - credential key id / jti uniqueness
   - credential expiry / revoked lookups
   - booking `(CreatedByOperatorId, RestaurantId, Date)`
4. Add follow-up migration only after implementation proves a stricter constraint is safe, for example making MCP-created rows require `CreatedByOperatorId`.

### Why this shape fits the repo
- It matches the existing EF Core + SQLite model style.
- It avoids overloading `AdminCredential`, which currently represents interactive admin users with long-lived login behavior and PVQ/reset fields.
- It preserves backwards-compatible upgrades and nullable rollout patterns already seen in existing migrations.

## Credential Format, Storage, And Lifecycle

### Recommendation
Use opaque bearer credentials with server-side lookup, not self-contained long-lived JWTs for agents.

### Proposed format
- Credential id / prefix for routing and audit, for example `ormcp_...`
- Secret shown only at issuance time
- Server stores only a hash/digest of the secret
- Requests present `Authorization: Bearer <opaque-token>`

### Why opaque over JWT for this feature
- Easier revocation without token blacklist complexity.
- Easier restaurant-scope changes without waiting for token expiry.
- Better fit for short-lived machine credentials and audit of last use.
- Avoids mixing agent credentials with current admin UI JWT semantics and cookie fallback.

### Lifecycle
- Issue: SuperAdmin or internal credential-management flow creates a short-lived credential bound to one operator and one or more restaurants.
- Store: persist digest, operator id, scope, issued-at, expires-at, revoked-at, and optional rate-limit bucket metadata.
- Use: MCP authenticates bearer token, loads credential record, validates expiry/revocation, resolves operator principal and restaurant scope.
- Rotate: issue new credential before expiry; revoke old one explicitly.
- Revoke: set `revoked_at`; enforcement must be immediate.
- Expire: hard fail after `expires_at`; no grace window beyond minimal clock skew tolerance.

### Recommended TTL
- Default hours, not days. Suggested MVP default: 8 hours, configurable downward/upward by admins.
- This is intentionally stricter than the current 30-day admin JWT behavior.

## Server-Side Authorization Matrix

| Capability | Required credential state | Restaurant scope check | Ownership check |
|---|---|---|---|
| Availability lookup | valid, unexpired, unrevoked | yes | no |
| Create reservation | valid, unexpired, unrevoked | yes | created row stamped with operator id |
| List own reservations | valid, unexpired, unrevoked | yes | filter `CreatedByOperatorId == operator_id` |
| Get own reservation | valid, unexpired, unrevoked | yes | same |
| Modify own reservation | valid, unexpired, unrevoked | yes | same |
| Cancel own reservation | valid, unexpired, unrevoked | yes | same |
| Escalate / hand-off | valid, unexpired, unrevoked | yes | own booking or explicit restaurant-scoped escalation target |

### Notes
- Existing broad admin role policies are not enough for MCP ownership rules.
- MCP authorization should use dedicated policies/handlers or explicit service-layer guards, not reuse `BookingsRead` / `BookingsWrite` alone.

## Minimal OpenResto Internal API Contract

This should be implemented as internal backend endpoints backing the MCP server. Reuse existing services where possible, but expose a narrower contract than the current public/admin mix.

### Proposed endpoints
- `GET /api/internal/operators/restaurants/{restaurantId}/availability?date=...&seats=...`
  - wraps existing availability logic
- `POST /api/internal/operators/restaurants/{restaurantId}/reservations`
  - creates reservation and stamps `CreatedByOperatorId`
- `GET /api/internal/operators/reservations`
  - filters to current operator, optional restaurant/date/status filters
- `GET /api/internal/operators/reservations/{id}`
  - current operator only
- `PATCH /api/internal/operators/reservations/{id}`
  - current operator only, scoped editable fields only
- `POST /api/internal/operators/reservations/{id}/cancel`
  - current operator only
- `POST /api/internal/operators/reservations/{id}/escalate`
  - creates hand-off event/note for human follow-up

### Contract principles
- Keep payloads close to existing booking/availability DTOs where compatible.
- Do not expose unrelated admin capabilities.
- Return stable machine-readable error codes plus human-readable message bodies.
- Keep restaurant id explicit in create/availability flows to simplify scope checks and audit.

## Remote MCP Transport, Auth, And Deployment Design

### Recommendation
Host the MCP as a remote HTTPS service on the VPS, fronted by the existing Nginx TLS terminator.

### Deployment options considered
1. Preferred MVP: add MCP HTTP endpoints to the existing ASP.NET Core backend or a tightly coupled ASP.NET Core module.
2. Secondary option: a small companion ASP.NET Core service in the same Compose stack behind Nginx.

### Preferred option rationale
- Reuses existing auth, EF Core models, migrations, logging, and test harness.
- Avoids introducing a second language/runtime and an extra operational surface area.
- Keeps internal API and MCP transport close to the booking domain logic.

### Edge design
- Nginx continues terminating HTTPS.
- Add a dedicated path such as `/mcp` or `/api/mcp` with stricter rate limiting than general public traffic.
- Forward bearer auth headers unchanged.
- Preserve `X-Forwarded-*` behavior already used by the backend.

### Transport auth
- Bearer token in `Authorization` header.
- No cookie auth for MCP.
- No reuse of current admin browser session cookie flow.

## Tool Schemas And Error Semantics

### Proposed MCP tools
- `lookup_availability`
  - input: `restaurant_id`, `date`, `seats`
  - output: slot list with availability and optional table/section hints
- `create_reservation`
  - input: `restaurant_id`, `date`, `seats`, `customer_name`, `customer_email`, optional `special_requests`, optional table/section or auto-assign preference
  - output: reservation id, booking reference, normalized UTC/local timestamps, status
- `list_own_reservations`
  - input: optional `restaurant_id`, date/status pagination filters
  - output: current operator’s reservations only
- `get_own_reservation`
  - input: reservation id
  - output: reservation detail
- `modify_own_reservation`
  - input: reservation id plus allowed mutable fields
  - output: updated reservation
- `cancel_own_reservation`
  - input: reservation id, optional reason
  - output: cancellation status
- `escalate_reservation`
  - input: reservation id, reason, optional hand-off note
  - output: escalation record id or accepted status

### Error semantics
- `401 Unauthorized`: missing, malformed, expired, or revoked credential.
- `403 Forbidden`: valid credential but restaurant scope or ownership violation.
- `404 Not Found`: reservation not visible to current operator or restaurant not in scope.
- `409 Conflict`: overlap, hold conflict, paused bookings, past booking cancellation, walk-in-only conflict.
- `422 Unprocessable Entity` or existing `400 BadRequest`: payload validation depending on current API style. Current repo mostly uses `400` for validation; keep that unless broader API normalization is approved.
- `429 Too Many Requests`: IP and credential bucket exceeded.
- `5xx`: unexpected server failures.

### Response shape recommendation
- Preserve current `{ message: "..." }` style for human-readable compatibility where practical.
- Add a stable code field for MCP consumers, for example `{ code, message, details? }`, on the new internal surface.

## Audit, Rate Limiting, Privacy, And Observability

### Audit
- Record credential id/jti, operator id, restaurant id, reservation id, action, outcome, request timestamp, and correlation/request id.
- For create/modify/cancel/escalate actions, persist a durable audit event or append-only log record.
- `last_used_at` should update on credentials, but avoid making authorization depend on that write succeeding.

### Rate limiting
- Keep existing IP-based ASP.NET/Nginx limits.
- Add credential-aware application limits for MCP endpoints, for example fixed-window per credential and optionally per operator.
- Apply stricter limits to reservation mutations than to availability reads.

### Privacy
- Only expose reservation/customer fields needed for the tool.
- Do not let one operator enumerate another operator’s reservations.
- Avoid leaking whether out-of-scope reservations exist; prefer `404` over `403` on resource fetches if that aligns with security policy.

### Observability
- Add structured logs with operator id, credential id, restaurant id, reservation id, and action.
- Add counters for auth failures, scope denials, ownership denials, create success/failure, modify/cancel success/failure, and escalation events.
- Add latency/error monitoring for MCP endpoints separately from public API traffic.

## Threat Model

### Primary threats
- Shared-token ambiguity causing incorrect reservation ownership.
- Credential leakage from agent environments.
- Overbroad restaurant scope enabling cross-location access.
- Replay/use of expired but cached credentials.
- Credential stuffing or brute-force against issuance or MCP endpoints.
- Reservation enumeration through predictable ids or response differences.
- Unauthorized mutation through missing server-side ownership checks.
- Audit blind spots that prevent incident reconstruction.

### Mitigations
- Separate durable operator identity from ephemeral credential.
- Opaque short-lived tokens with server-side revocation.
- Explicit restaurant scope checks on every request.
- Dedicated MCP auth path without cookie fallback.
- Uniform not-found behavior for out-of-scope resources when appropriate.
- Credential and IP rate limiting.
- Structured audit logs and revocation support.

### Known residual risk
- In-memory holds remain single-instance only. This is acceptable for the current VPS/single-stack model, but should be documented as a scaling limit for future multi-instance deployment.

## Alternatives Considered

### Reuse current admin JWTs directly
Rejected for MVP because they are 30-day, role-only, not restaurant-scoped, and do not solve shared-agent-token ownership ambiguity.

### Store ownership by credential id instead of operator id
Rejected because rotated/reissued credentials would break durable "own bookings" semantics and complicate support workflows.

### Extend `AdminCredential` to also represent machine credentials
Rejected for MVP because it mixes interactive admin account lifecycle with agent credential lifecycle and creates awkward schema/behavior coupling.

### Build a separate non-.NET MCP service first
Deferred. Possible later, but it adds operational and domain duplication with no clear MVP benefit in this repo.

## Phased Implementation Plan

### Phase 1: Credential and identity foundation
- Add operator principal, scope, and credential tables.
- Add issuance/revocation service and tests.
- Add dedicated MCP auth middleware/service path.

### Phase 2: Booking ownership
- Add booking ownership fields and indexes.
- Stamp MCP-created reservations with `CreatedByOperatorId`.
- Add service-layer ownership filters and authorization tests.

### Phase 3: Internal operator API
- Implement narrow internal reservation endpoints.
- Reuse existing availability and booking domain services where possible.
- Add stable error codes and structured audit hooks.

### Phase 4: MCP transport and VPS exposure
- Implement remote MCP endpoint/tool handlers.
- Add Nginx route/rate-limit configuration for MCP path.
- Add deployment docs for VPS-hosted MCP access.

### Phase 5: Hardening and rollout
- Audit/observability polish.
- Negative security tests and rate-limit tests.
- Operational runbooks for issuing, rotating, and revoking credentials.

## Acceptance Criteria
- A valid scoped MCP credential can query availability for restaurants in scope over HTTPS on the VPS-hosted deployment path.
- A created reservation is stamped with the creator operator identity.
- The creating operator can list/get/modify/cancel that reservation through MCP.
- Another operator, even with access to the same restaurant, cannot list/get/modify/cancel that reservation unless a future explicit sharing rule is introduced.
- Expired or revoked credentials fail immediately.
- Out-of-scope restaurant requests fail closed.
- All new persistence and auth behavior are covered by automated tests aligned with existing backend patterns.
- Public booking and existing admin UI flows remain intact.

## Concrete Test Plan

### Migration and model tests
- Verify additive migration creates new operator/credential tables and booking ownership columns.
- Verify existing databases upgrade without breaking legacy bookings.
- Verify indexes/uniqueness and nullability assumptions.

### Service tests
- Credential issuance, expiry, revocation, and scope resolution.
- Booking creation stamps operator id.
- Ownership checks allow self and deny others.
- Credential-aware rate-limit partitioning logic.

### Controller/integration tests
- Auth success/failure for opaque MCP credentials.
- Availability allowed only for in-scope restaurants.
- Create/list/get/update/cancel own reservation happy paths.
- Cross-operator access returns the expected denial semantics.
- Expired/revoked credential cases.
- Audit event emission for mutations and escalation.

### End-to-end/API tests
- HTTPS path through Nginx to MCP/internal endpoints in VPS-like Compose topology.
- Regression tests proving public booking endpoints and current admin auth still work.
- Negative tests for malformed tokens, restaurant scope mismatch, and replay after revocation.

## Recommended Next Step
Run `gsd-discuss-phase internal-operator-mcp` and convert the open questions in `.planning/STATE.md` into explicit implementation decisions before writing code or migrations.
