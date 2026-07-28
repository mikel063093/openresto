# WhatsApp Reservations Test Context

## Status
- Requested on Tuesday, July 28, 2026 as a planning-only GSD pass.
- Scope is limited to the WhatsApp Business reservation channel in test only.
- This document reconciles the current branch state against `develop` and defines the remaining execution path without implementing code in this planning turn.

## Planning Inputs Read
- `.planning/PROJECT.md`
- `.planning/REQUIREMENTS.md`
- `.planning/ROADMAP.md`
- `.planning/STATE.md`
- `.planning/features/whatsapp-reservations-test-foundation/CONTEXT.md`
- `.planning/features/whatsapp-reservations-test-foundation/PLAN.md`
- commits since `develop`

## Branch And Commit Assessment Since `develop`

### Branch
- Current branch: `feat/whatsapp-reservations-test`
- `develop` tip resolved locally to `f047f0862cbdbcfbee700b4b86b86736ebbafb79`

### Commits assessed
1. `7b8d713 feat: add whatsapp reservation foundation persistence`
   - Added booking phone ownership fields, occasion catalog entities, booking snapshot entity, idempotency entity, EF migrations, admin catalog controller, and focused tests.
2. `4e93b85 fix: harden whatsapp reservation test foundation review fixes`
   - Hardened occasion snapshot uniqueness and idempotency behavior with follow-up migration/tests.
3. `814db6d fix(api): validate booking reschedule conflicts`
   - Tightened booking reschedule conflict checks in `BookingService`.
4. `5f98d27 feat(api): add private whatsapp reservation channel`
   - Added private WhatsApp-authenticated list/get/update/cancel/catalog endpoints and integration coverage.
5. `e53006a fix(api): harden whatsapp channel security and idempotency`
   - Added assertion replay protection, private-route proxy blocking, config validation, stricter idempotency handling, and more tests.

### Net effect of branch work relative to `develop`
- OpenResto-side persistence foundation exists on this branch.
- OpenResto private WhatsApp auth/assertion handling exists on this branch.
- OpenResto private WhatsApp list/get/update/cancel/catalog endpoints exist on this branch.
- Public Nginx configs now explicitly deny `/api/private/channels/whatsapp/`.
- The branch does not yet add:
  - a bot service project
  - an n8n test stack or durable workflow storage
  - Meta webhook verification workflows
  - an atomic WhatsApp availability-plus-create reservation operation
  - admin Expo UI for WhatsApp catalog/handoff settings
  - test-only Docker/Traefik/DNS additions for `n8n-test` and `reservation-bot-test`
  - Meta/WABA provisioning runbook artifacts
  - full end-to-end sandbox and observability plan execution

## Locked Decisions From User Request

### Environment scope
- Test only:
  - `test-rest.joypaw.tech`
  - `n8n-test.joypaw.tech`
  - `reservation-bot-test.joypaw.tech`
- No production deployment, push, secret creation, or production/test infra mutation in this planning pass.
- No promotion path to production is included in this feature.

### Authority and trust boundaries
- `n8n` validates Meta and is the only assertion issuer.
- `OpenResto` remains the booking authority.
- Private WhatsApp API remains blocked from the public proxy.
- Existing operator MCP stays operator-owned; it is not the customer ownership boundary.
- Any reuse of operator MCP is limited to availability/create internals only if the final implementation still preserves OpenResto booking authority.

### Product behavior
- Customer identity is the verified WhatsApp `wa_id`, normalized to E.164.
- Email is mandatory before reservation creation completes.
- Create/change/cancel actions require explicit confirmation.
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
- No payments or deposits are in scope.
- Extras/add-ons must be copied as immutable booking-time snapshots with estimated COP pricing.
- Human handoff goes to a WhatsApp destination, not internal email.

## Verified Repo Baseline

### Existing backend capability on this branch
- Private WhatsApp route exists at `/api/private/channels/whatsapp`.
- Current implemented private API supports:
  - list own reservations
  - get own reservation
  - update own reservation
  - cancel own reservation
  - get active occasion catalog by restaurant
- Current auth design requires:
  - internal caller bearer credential
  - signed assertion header
  - issuer, audience, scope, action checks
  - replay prevention via `ChannelMutationIdempotencyRecords`
- Current persistence already includes:
  - `Bookings.CustomerPhoneE164`
  - `Bookings.CustomerPhoneNormalized`
  - `Bookings.CreatedViaChannel`
  - `RestaurantOccasionCatalogItems`
  - `BookingOccasionSnapshots`
  - `ChannelMutationIdempotencyRecords`

### Existing gaps confirmed in repo
- No route yet creates a WhatsApp-owned reservation atomically with:
  - availability check
  - booking creation
  - required email
  - immutable add-on snapshots
  - idempotency
  - concurrency guarantees
- No `OpenRestoReservationBot/` project exists.
- No `n8n/` test stack exists in the repo today.
- No admin Expo UI exists yet for:
  - occasion catalog management
  - WhatsApp visibility toggle
  - handoff WhatsApp configuration
- `docker-compose.test-rest.yml` and `traefik/test-rest.yml` currently expose only backend, frontend, and reverse proxy.
- No checked-in Meta/WABA runbook exists.

## Trust Boundary Model

### Public internet boundary
- Meta calls only `n8n-test.joypaw.tech`.
- No public caller reaches `/api/private/channels/whatsapp/`.
- `test-rest.joypaw.tech` public proxy must continue returning `404` for private WhatsApp paths.

### Internal test network boundary
- `n8n-test` may call `reservation-bot-test`.
- `reservation-bot-test` may call:
  - OpenResto private WhatsApp API
  - OpenResto availability/create internals
- `reservation-bot-test` must not mint its own identity assertions.
- `reservation-bot-test` must not have Meta or OpenAI credentials.

### Booking authority boundary
- OpenResto decides:
  - who owns a reservation
  - whether a requested slot can be booked
  - whether a mutation is idempotent or stale
  - what add-on snapshots are persisted
- n8n and bot logic may prepare or request operations, but never become the final reservation authority.

## Security Baseline And Gap

### What is already present
- Shared-secret assertion verification with issuer/audience/scope/action validation.
- Replay detection on assertion `jti`.
- Idempotency persistence for private API writes.
- Public proxy denial for private WhatsApp API routes.

### What still requires design/execution
- Key rotation support for the assertion-signing path.
- Decision on staying with symmetric HMAC for test versus asymmetric signing migration.
- Durable n8n session and dedupe storage.
- PII redaction and replay-safe workflow payload storage outside OpenResto.
- Bot-service authentication and request signing between n8n and bot, and bot to OpenResto.
- Full security review of secrets placement and Docker/network exposure.

## HMAC Rotation Versus Asymmetric Migration Tradeoff

### Preferred test-phase approach
- Keep a symmetric assertion issuer/verifier path for test MVP, but require rotation-ready design:
  - include `kid` on assertions
  - support one active key plus one previous verification key in OpenResto
  - rotate by introducing a new active key in n8n, keeping the old key in OpenResto until old assertions expire

### Why this is acceptable in test
- There is exactly one issuer (`n8n`) and one verifier (`OpenResto`).
- HS256/HMAC is simpler to ship quickly and easier to operate in a single isolated test stack.
- Existing branch code already uses a symmetric signing key; the least-risk execution path is to harden it rather than re-platform it mid-stream.

### Why asymmetric signing may still be preferred later
- With symmetric signing, issuer and verifier share the same secret; compromise of either side allows forging assertions.
- RS256 or EdDSA reduces verifier-side blast radius because OpenResto would keep only the public key.
- Asymmetric signing scales better if more verifiers or separate environments are introduced.

### Execution implication
- This Level-C plan must implement rotation-ready assertion contracts now.
- It should keep a narrow migration seam so a later phase can swap OpenResto verification from shared secret to public-key verification without changing the bot/n8n business contract.

## Execution-Shaping Assumptions
- Existing OpenResto branch work is treated as partially complete but not sufficient to close the feature.
- The final implementation should avoid introducing managed external services outside test Docker unless a later plan explicitly records that decision.
- Durable n8n state may be satisfied by a test-scoped database volume or a dedicated SQLite/Postgres container if that choice is made explicitly during execution.
- User-facing and operator-facing text for WhatsApp and admin operational flows should be written in `es-CO`.

## Out Of Scope
- Production rollout or promotion.
- End-customer mobile/web login based on phone identity.
- Payments, deposits, invoicing, or reconciliation.
- General-purpose public exposure of the private channel API.
- Multi-tenant tenant routing beyond the single-tenant test environment.
