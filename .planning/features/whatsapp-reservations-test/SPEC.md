# whatsapp-reservations-test

## Status
Decision-ready Level-C feature spec for the isolated WhatsApp reservation channel in test.

## Objective
Complete a test-only WhatsApp Business reservation channel where Meta webhook traffic is received and validated by `n8n-test`, intent execution is constrained through `reservation-bot-test`, and OpenResto remains the only booking authority for customer-owned reservations.

## Scope

### In scope
- Atomic availability plus create reservation operation for WhatsApp in test.
- Trusted WhatsApp ownership enforcement inside OpenResto.
- Immutable extras snapshots with estimated COP values.
- No-payment reservation flow.
- `reservation-bot` internal service with fixed contract and no model-side secrets.
- n8n test stack with durable session, dedupe, and idempotency storage.
- Meta GET/POST webhook verification, HMAC verification, event ordering, dedupe, structured LLM outputs, confirmation state machine, templates/messages, guardrails, PII redaction, and handoff workflows.
- Admin Expo UI plus backend settings support for catalog and WhatsApp/handoff settings.
- Test-only Docker, Traefik, DNS, and secrets-injection checklist for `n8n-test` and `reservation-bot-test`.
- Meta/WABA provisioning runbook with external prerequisites marked `USER/AWAITING`.
- Comprehensive tests, migrations, security review, E2E sandbox, smoke, and observability.

### Out of scope
- Production rollout.
- Payments/deposits.
- Public exposure of the OpenResto private WhatsApp API.
- Replacing the existing public booking flow.
- Allowing customer WhatsApp flows to access arbitrary operator MCP tools.

## Non-Negotiable Invariants
- `n8n` validates Meta and is the only assertion issuer.
- OpenResto remains the booking authority.
- Private API stays blocked from the public proxy.
- Reservation ownership for WhatsApp self-service is enforced server-side by verified normalized phone.
- Mutations are explicit-confirmation only.
- No Meta/OpenAI/MCP secrets are ever exposed to models, end users, or public logs.
- All planning and execution remain test-only.

## User Journeys

### Availability
1. Customer writes on WhatsApp.
2. n8n verifies Meta authenticity and normalizes sender identity.
3. n8n collects structured inputs with LLM assistance but no tools.
4. n8n calls `reservation-bot-test` with a fixed operation request.
5. `reservation-bot-test` calls a fixed OpenResto read path for availability.
6. n8n replies in `es-CO`.

### Create reservation
1. Customer selects restaurant, date, time, seats, email, and optional extras.
2. n8n presents a full summary in `es-CO`.
3. Customer explicitly confirms.
4. n8n calls `reservation-bot-test` with confirmation evidence and idempotency key.
5. Bot calls a single OpenResto atomic create operation.
6. OpenResto re-checks availability and writes booking, ownership, channel metadata, and add-on snapshots in one authority path.
7. n8n replies with confirmation message in `es-CO`.

### Self-service list/change/cancel
1. Customer asks to see or manage reservations.
2. n8n requests a constrained bot operation.
3. Bot calls OpenResto private WhatsApp API using the trusted phone assertion chain.
4. OpenResto enforces phone ownership and concurrency/idempotency.
5. n8n returns structured `es-CO` copy.

### Handoff
1. Unsupported or escalated conversation reaches handoff.
2. n8n records a redacted handoff event through OpenResto.
3. n8n forwards a sanitized summary to configured human WhatsApp destination.
4. Customer gets an `es-CO` acknowledgement that a human will continue.

## Required Domain Model

### Existing branch baseline to preserve
- `Booking.CustomerPhoneE164`
- `Booking.CustomerPhoneNormalized`
- `Booking.CreatedViaChannel`
- `RestaurantOccasionCatalogItem`
- `BookingOccasionSnapshot`
- `ChannelMutationIdempotencyRecord`

### Additional required persistence
- Restaurant-level WhatsApp settings
  - `IsWhatsAppTestEnabled`
  - `HandoffWhatsAppE164`
  - optional per-location visible display name/template settings
- Channel session storage
  - durable session id
  - verified phone
  - last processed Meta message id/timestamp
  - workflow state
  - confirmation pending state
  - idempotency correlation
- Channel event ledger
  - inbound Meta event id
  - ordering watermark
  - dedupe status
  - redacted payload reference
- Handoff audit record
  - booking id nullable
  - restaurant id
  - verified phone snapshot
  - summary snapshot
  - handoff destination snapshot
  - created at

### Data invariants
- Booking-time extras snapshot rows are immutable after create.
- A booking created from WhatsApp must store:
  - verified phone ownership
  - required email
  - `CreatedViaChannel = "whatsapp"`
- A mutation idempotency key may only be replayed with the exact same fingerprint.
- Assertion `jti` replay must fail closed.

## API And Contract Design

### OpenResto private WhatsApp API
- Authn
  - internal caller credential
  - signed assertion minted only by n8n
  - action + scope + issuer + audience + `kid` validation
- Public exposure
  - never routed through public reverse proxy

### Required OpenResto endpoints
- Existing branch endpoints to retain
  - `GET /api/private/channels/whatsapp/reservations`
  - `GET /api/private/channels/whatsapp/reservations/{id}`
  - `PATCH /api/private/channels/whatsapp/reservations/{id}`
  - `POST /api/private/channels/whatsapp/reservations/{id}/cancel`
  - `GET /api/private/channels/whatsapp/restaurants/{restaurantId}/occasion-catalog`
- Required new endpoints
  - `GET /api/private/channels/whatsapp/restaurants`
  - `POST /api/private/channels/whatsapp/reservations`
    - atomic availability + create
    - required email
    - optional extras snapshot ids
    - idempotency key
    - confirmation evidence
    - expected concurrency fingerprint where applicable
  - `POST /api/private/channels/whatsapp/handoffs`
  - `PUT /api/admin/restaurants/{restaurantId}/whatsapp-settings`

### Atomic create contract
- Request
  - restaurant id
  - date/time
  - seats
  - customer name
  - customer email
  - optional notes
  - selected occasion catalog item ids
  - verified phone assertion context
  - idempotency key
  - confirmation token/session reference
- Authority behavior
  - validate restaurant visibility and WhatsApp test toggle
  - validate email present and well formed
  - normalize phone from trusted assertion only
  - validate extras belong to restaurant and are active or explicitly allowed for snapshotting
  - re-check availability inside the create authority path
  - persist booking and snapshots once
  - replay same result on same idempotency key + fingerprint
  - reject same key with different fingerprint

### Reservation-bot fixed contract
- n8n may call bot only through these eight bounded operations:
  - `availability`
  - `create`
  - `list`
  - `detail`
  - `update`
  - `cancel`
  - `occasionCatalog`
  - `handoff`
- Bot may not accept:
  - arbitrary URLs
  - arbitrary MCP tool names
  - arbitrary OpenResto route names
  - raw provider credentials

### Bot-to-OpenResto contract
- Bot forwards:
  - trusted assertion from n8n
  - internal caller credential or bot-scoped service credential
  - idempotency key
  - correlation ids
  - operation name
- Bot never mints assertions.

## Assertion Security Decision

### Test-phase decision
- Keep symmetric assertion signing for this feature, but make it rotation-ready.

### Required implementation details
- Assertions must contain:
  - `iss`
  - `aud`
  - `sub` as verified phone
  - `jti`
  - `scope`
  - `action`
  - `kid`
  - short `exp`
- OpenResto config must support:
  - active verification key
  - previous verification key
  - accepted key ids
- n8n must issue using only the active key.
- Rotation process:
  1. Add new verification key to OpenResto.
  2. Switch n8n active signing key and `kid`.
  3. Wait for max assertion TTL to expire.
  4. Remove previous key from OpenResto.

### Migration seam for asymmetric signing
- Keep signer/verifier abstraction at the assertion component boundary.
- Future migration may swap to RS256 or EdDSA with:
  - private key only in n8n
  - public key only in OpenResto
  - same claim contract

### Tradeoff summary
- Symmetric now:
  - simpler
  - fewer moving parts
  - faster to ship in test
  - larger shared-secret blast radius
- Asymmetric later:
  - stronger separation of issuer and verifier trust
  - better long-term rotation posture
  - slightly more operational complexity

## Durable State Requirements

### n8n durable storage
- Must survive container restart.
- Must support:
  - session state
  - event dedupe
  - message ordering watermark
  - confirmation pending state
  - outbound idempotency correlation
  - replay/debug artifacts with PII redaction

### OpenResto durable storage
- Must store:
  - booking authority records
  - mutation idempotency
  - add-on snapshots
  - handoff audit
  - restaurant WhatsApp settings

## Admin Surface Requirements

### Backend admin settings
- SuperAdmin-only management for:
  - per-restaurant WhatsApp test visibility toggle
  - per-restaurant handoff WhatsApp number
  - occasion catalog CRUD

### Expo admin UI
- Settings screen additions in `es-CO`:
  - location-level WhatsApp test section
  - handoff configuration card
  - occasion catalog card with CRUD rows
- UI must preserve current admin patterns and auth model.

## Deployment Boundaries

### Public endpoints allowed
- `https://test-rest.joypaw.tech`
- `https://n8n-test.joypaw.tech`
- `https://reservation-bot-test.joypaw.tech`

### Private-only routes
- `/api/private/channels/whatsapp/*` reachable only on internal network paths.
- No Traefik/Nginx public route may forward them from the public edge.

### Test-only stack rules
- All new compose services and secrets wiring must be isolated to test files.
- No modifications to production compose or production Traefik are permitted in this feature.
- No production promotion checklist is included.

## `es-CO` Copy Requirement

### Customer-facing examples
- Confirmation prompt:
  - `Vas a reservar para 4 personas en {restaurante} el {fecha} a las {hora}. ¿Confirmas la reserva?`
- Missing email:
  - `Para completar la reserva necesito tu correo electrónico.`
- Handoff:
  - `Te voy a comunicar con una persona del restaurante para continuar por este mismo WhatsApp.`

### Operator-facing admin examples
- `Canal de WhatsApp de prueba`
- `Número de handoff`
- `Catálogo de ocasiones`
- `Precio estimado`

## Acceptance Criteria
- Customer can complete availability, create, list, change, cancel, and handoff flows in test using WhatsApp.
- Create path is atomic and idempotent.
- Booking ownership is always derived from trusted WhatsApp assertion, never user-entered phone text.
- Private OpenResto API remains unreachable from the public proxy.
- Meta/OpenAI/MCP secrets are not exposed in bot, model, logs, or workflow exports.
- All operational copy exposed in the feature is `es-CO`.
- Full verification suite passes in test with no production rollout step.
