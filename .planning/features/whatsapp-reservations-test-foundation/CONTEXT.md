# WhatsApp Reservations Test Foundation Context

## Source And Status
- Requested on Tuesday, July 28, 2026 as a planning-only phase for the existing `develop` branch codebase.
- This feature is not yet registered in `.planning/ROADMAP.md`; the user-supplied slug `whatsapp-reservations-test-foundation` is treated as the planning anchor for these artifacts.
- Repository evidence was taken from the current ASP.NET Core backend, Expo admin frontend, existing operator MCP implementation, tests, and the isolated `test-rest` deployment files.

## Verified Repo Baseline
- The backend already exposes operator-scoped MCP at `/api/mcp/operator` plus authenticated internal operator reservation APIs and tests.
- Reservation create/availability logic already exists in `BookingService` and `AvailabilityService`.
- Admin location settings already exist in the Expo admin UI and backend restaurant update flow.
- `docker-compose.test-rest.yml` and `traefik/test-rest.yml` already define the isolated `test-rest.joypaw.tech` environment.
- There is no customer phone ownership model, no WhatsApp webhook surface, no channel session model, no per-location occasion/decor catalog, and no chatbot orchestration service in the repo today.

## Locked Product Decisions

### Scope
- This is test-only WhatsApp Cloud API work for:
  - `test-rest.joypaw.tech`
  - `n8n-test.joypaw.tech`
  - `reservation-bot-test.joypaw.tech`
- One tenant owns all restaurant locations.
- Do not target `main`; base planning on the current `develop`-style repo state already containing operator MCP.

### Reservation behavior
- Customer identity is the incoming WhatsApp E.164 number from Meta.
- Customers may:
  - check availability
  - create a reservation
  - list their own reservations
  - change only date, time, and seats
  - cancel their own reservations
- Customers may not change:
  - name
  - email
  - location
  - notes
- Email is required before reservation creation completes.
- Reservations are automatically confirmed when capacity exists.
- Free-form notes are supported.

### Channel boundaries
- The existing operator-owned MCP surface must not become the ownership boundary for customer self-service.
- Existing `/api/mcp/operator` may be reused only for availability/create behavior.
- Customer list/update/cancel flows require a new private channel API where OpenResto enforces phone ownership server-side.
- Handoff escalates to a human WhatsApp, not email or an internal-only queue.

### Commercial add-ons
- Each location needs an admin-managed catalog for birthday, anniversary, decorations, and similar items.
- Catalog items store estimated prices in COP.
- Reservation records must keep immutable booking-time snapshots of the selected catalog items and their estimated prices.
- No payments, deposits, or advance collection are in scope.

### Security and privacy
- Validate Meta webhook authenticity with HMAC verification.
- Enforce idempotency for inbound webhook/event processing and outbound mutation commands.
- Mutating actions require explicit confirmation in the conversation before execution.
- No LLM tool access and no model-side credentials/tokens.
- Redact PII and secrets in logs, workflow payloads, and replay artifacts.

### AI/provider direction
- Meta official WhatsApp Cloud API is the WhatsApp transport.
- OpenAI is the initial language-model provider, behind an internal provider abstraction so the bot service is not hard-wired to one vendor.

## Architecture Decisions For This Phase

### Runtime split
- `n8n-test.joypaw.tech` is the public Meta webhook receiver and primary orchestration flow: it verifies the Meta signature, deduplicates inbound events, keeps conversation state, invokes the provider-neutral LLM step, and calls only fixed `reservation-bot` operations.
- `reservation-bot-test.joypaw.tech` is a separate constrained service in the same repo. It validates structured intents and confirmations, owns no Meta webhook or LLM credentials, and calls OpenResto only through fixed server-to-server operations.
- `test-rest.joypaw.tech` remains the OpenResto source of truth for bookings, customer ownership, admin catalog management, availability, idempotent mutations, and audit records. n8n is never the authority for reservation ownership or booking writes.

### OpenResto authority
- OpenResto must own:
  - normalized phone identity
  - customer reservation ownership enforcement
  - idempotent booking writes
  - immutable add-on snapshots
  - audit logging
- The bot service may propose actions, but OpenResto remains the write authority.

### Customer ownership model
- Booking records gain a normalized customer phone field distinct from operator ownership.
- WhatsApp self-service authorization is based on the verified E.164 phone plus restaurant scope implied by the booking record.
- Existing public email/ref lookup remains backward compatible and unchanged for non-WhatsApp flows.

### Conversation model
- n8n is the conversation orchestrator and LLM caller. It sends only a schema-validated intent plus trusted sender context to `reservation-bot`; it cannot choose arbitrary MCP methods or OpenResto routes.
- Conversation flow is confirmation-driven:
  - gather intent and structured inputs
  - show summarized action
  - require explicit user confirmation
  - execute exactly once
- List/update/cancel must operate against persisted OpenResto data, not conversational memory.

## Required Codebase Fits
- Reuse `BookingService`, `AvailabilityService`, and existing booking validation rules instead of forking reservation logic.
- Keep additive EF Core migrations with snapshot updates and migration tests.
- Fit new admin catalog settings into the current restaurant/admin settings patterns in backend and Expo frontend.
- Keep the existing operator MCP and admin browser auth behavior intact.
- Keep deployment additive to the existing test-rest Compose and Traefik topology.

## Assumptions Used To Complete Planning
- The new planning artifacts may live under `.planning/features/whatsapp-reservations-test-foundation/` even though the roadmap has not yet been updated.
- Human handoff starts with a single configured WhatsApp destination per location; tenant-wide fallback is acceptable for MVP if the per-location number is missing.
- The verified Meta sender phone (`wa_id`) is the canonical customer phone identity stored in OpenResto after normalization to E.164.
- The bot service uses server-side provider credentials only; no credential is ever exposed to the model or sent back through n8n/OpenResto responses.
- Archived restaurants are excluded from WhatsApp booking selection in the test environment.

## Deferred / Explicitly Out Of Scope
- Payments, deposits, and payment reconciliation.
- Multi-tenant routing.
- General customer web/mobile account login based on phone ownership.
- Replacing the existing public booking flow.
- Broad production rollout hardening outside the isolated test domains.
