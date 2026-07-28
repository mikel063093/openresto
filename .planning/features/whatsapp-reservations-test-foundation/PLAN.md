# WhatsApp Reservations Test Foundation Plan

## Goal
Add a test-only WhatsApp reservation channel on top of the current OpenResto backend and admin UI, using Meta WhatsApp Cloud API, a separate bot orchestration service with provider abstraction, OpenResto-enforced phone ownership, immutable add-on snapshots, explicit confirmations, and isolated deployment at `test-rest.joypaw.tech`, `reservation-bot-test.joypaw.tech`, and `n8n-test.joypaw.tech`.

## Non-Negotiable Constraints
- Planning only in this phase; no production source edits are performed here.
- Reuse existing OpenResto booking/availability rules instead of inventing parallel reservation logic.
- Existing `/api/mcp/operator` remains operator-owned and is never used for customer list/update/cancel authorization.
- All WhatsApp mutations must be idempotent and confirmation-gated.
- Logs, traces, and workflow payloads must redact phone numbers, email addresses, tokens, and free-form notes by default.

## Delivery Shape

### Service split
- `OpenRestoApi/` remains the source of truth and gains:
  - private phone-owned reservation API
  - admin catalog/config APIs
  - new persistence model for verified WhatsApp phone ownership, idempotent mutations, audits, and booking add-on snapshots
- New `OpenRestoReservationBot/` service handles:
  - schema and confirmation validation for n8n-supplied intents
  - fixed server-to-server calls into OpenResto
  - optional MCP usage for availability/create only
  - no Meta webhook endpoint, no LLM provider credential, and no free-form tool selection
- `n8n` is the primary Meta webhook, conversation, LLM and handoff workflow surface, but not the booking authority. It may invoke only fixed authenticated bot-service operations and never holds an OpenResto MCP credential.

### Recommended execution order
1. Persistence and ownership foundation in OpenResto
2. Private customer channel API in OpenResto
3. Bot service contract and provider-neutral intent schema
4. n8n Meta webhook, verification, conversation, LLM and handoff workflows
5. Admin catalog/config UI and APIs
6. OpenResto audit/idempotency integration for bot mutation commands
7. Test-rest Docker and Traefik rollout
8. End-to-end verification

## Exact File Plan

### A. OpenResto persistence and domain

#### Update existing files
- `OpenRestoApi/Core/Domain/Booking.cs`
- `OpenRestoApi/Core/Application/DTOs/BookingDto.cs`
- `OpenRestoApi/Core/Application/DTOs/AdminDto.cs`
- `OpenRestoApi/Core/Application/Mappings/BookingMapper.cs`
- `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`
- `OpenRestoApi/Core/Application/Services/BookingService.cs`
- `OpenRestoApi/Core/Application/Services/RestaurantManagementService.cs`
- `OpenRestoApi/Core/Application/DTOs/RestaurantDto.cs`

#### Add domain files
- `OpenRestoApi/Core/Domain/RestaurantOccasionCatalogItem.cs`
- `OpenRestoApi/Core/Domain/BookingOccasionSnapshot.cs`
- `OpenRestoApi/Core/Domain/ChannelMutationIdempotencyRecord.cs`

#### Add/extend repository contracts
- `OpenRestoApi/Core/Application/Interfaces/IRestaurantOccasionCatalogRepository.cs`
- `OpenRestoApi/Core/Application/Interfaces/IBookingOccasionSnapshotRepository.cs`
- `OpenRestoApi/Core/Application/Interfaces/IChannelMutationIdempotencyRepository.cs`

#### Add repository implementations
- `OpenRestoApi/Infrastructure/Persistence/Repositories/RestaurantOccasionCatalogRepository.cs`
- `OpenRestoApi/Infrastructure/Persistence/Repositories/BookingOccasionSnapshotRepository.cs`
- `OpenRestoApi/Infrastructure/Persistence/Repositories/ChannelMutationIdempotencyRepository.cs`

### B. OpenResto private customer channel API

#### Add DTO files
- `OpenRestoApi/Core/Application/DTOs/WhatsAppChannelDtos.cs`
- `OpenRestoApi/Core/Application/DTOs/RestaurantOccasionCatalogDtos.cs`

#### Add service files
- `OpenRestoApi/Core/Application/Services/WhatsAppPhoneOwnershipService.cs`
- `OpenRestoApi/Core/Application/Services/WhatsAppReservationChannelService.cs`
- `OpenRestoApi/Core/Application/Services/WhatsAppHandoffService.cs`
- `OpenRestoApi/Core/Application/Services/ChannelIdempotencyService.cs`

#### Add auth/support files
- `OpenRestoApi/Infrastructure/Auth/WhatsAppChannelAuthenticationDefaults.cs`
- `OpenRestoApi/Infrastructure/Auth/WhatsAppChannelAuthenticationHandler.cs`
- `OpenRestoApi/Infrastructure/Security/PiiRedactor.cs`

#### Add controllers
- `OpenRestoApi/Controllers/WhatsAppChannelReservationsController.cs`
- `OpenRestoApi/Controllers/AdminOccasionCatalogController.cs`

#### Update startup wiring
- `OpenRestoApi/Extensions/ServiceCollectionExtensions.cs`
- `OpenRestoApi/Program.cs`

### C. Bot orchestration service

#### Add new project
- `OpenRestoReservationBot/OpenRestoReservationBot.csproj`
- `OpenRestoReservationBot/Program.cs`
- `OpenRestoReservationBot/appsettings.json`
- `OpenRestoReservationBot/appsettings.Development.json`

#### Add orchestration files
- `OpenRestoReservationBot/Contracts/InboundWhatsAppEvent.cs`
- `OpenRestoReservationBot/Contracts/OutboundWhatsAppMessage.cs`
- `OpenRestoReservationBot/Contracts/OpenRestoChannelApiDtos.cs`
- `OpenRestoReservationBot/Controllers/BotOperationsController.cs`
- `OpenRestoReservationBot/Services/ConversationOrchestrator.cs`
- `OpenRestoReservationBot/Services/IntentClassifier.cs`
- `OpenRestoReservationBot/Services/ConfirmationStateMachine.cs`
- `OpenRestoReservationBot/Services/OpenRestoChannelClient.cs`
- `OpenRestoReservationBot/Services/OpenRestoOperatorMcpClient.cs`
- `OpenRestoReservationBot/Providers/IChatProvider.cs`
- `OpenRestoReservationBot/Providers/OpenAiChatProvider.cs`
- `OpenRestoReservationBot/Providers/ProviderResponseSanitizer.cs`
- `OpenRestoReservationBot/Logging/RedactingLoggerScopes.cs`

### D. Admin Expo frontend

#### Update existing admin screens/API layer
- `openresto-frontend/app/admin/settings.tsx`
- `openresto-frontend/components/admin/settings/LocationCard.tsx`
- `openresto-frontend/components/admin/settings/RestaurantInfoForm.tsx`
- `openresto-frontend/components/admin/settings/settings.styles.ts`
- `openresto-frontend/api/admin.ts`

#### Add new admin components
- `openresto-frontend/components/admin/settings/OccasionCatalogCard.tsx`
- `openresto-frontend/components/admin/settings/OccasionCatalogRow.tsx`
- `openresto-frontend/components/admin/settings/HandoffWhatsAppCard.tsx`

### E. Deployment and workflow

#### Update root solution/repo files
- `openresto.sln`
- `docker-compose.test-rest.yml`
- `traefik/test-rest.yml`
- `package.json`

#### Add deployment support files
- `OpenRestoReservationBot/Dockerfile`
- `n8n/test/README.md`
- `n8n/test/workflows/whatsapp-reservations-inbound.json`
- `n8n/test/workflows/whatsapp-human-handoff.json`
- `n8n/test/workflows/whatsapp-replay-and-observability.json`
- `.env.example` updates only for variable names, never real values

## Migration Plan

### Migration 1: WhatsApp customer channel foundation
- Files:
  - `OpenRestoApi/Migrations/<timestamp>_AddWhatsAppCustomerChannelFoundation.cs`
  - `OpenRestoApi/Migrations/<timestamp>_AddWhatsAppCustomerChannelFoundation.Designer.cs`
  - `OpenRestoApi/Migrations/AppDbContextModelSnapshot.cs`
- Schema changes:
  - add `Bookings.CustomerPhoneE164`
  - add `Bookings.CustomerPhoneNormalized`
  - add `Bookings.CreatedViaChannel` extension values for WhatsApp channel
  - add `ChannelMutationIdempotencyRecords`
- Indexes:
  - `(CustomerPhoneNormalized, RestaurantId, Date)`
  - unique idempotency key per action/session

### Migration 2: Occasion catalog and immutable booking snapshots
- Files:
  - `OpenRestoApi/Migrations/<timestamp>_AddOccasionCatalogAndBookingSnapshots.cs`
  - `OpenRestoApi/Migrations/<timestamp>_AddOccasionCatalogAndBookingSnapshots.Designer.cs`
  - `OpenRestoApi/Migrations/AppDbContextModelSnapshot.cs`
- Schema changes:
  - add `RestaurantOccasionCatalogItems`
  - add `BookingOccasionSnapshots`
  - add per-location human handoff WhatsApp config if kept in the restaurant aggregate or a companion config table
- Invariants:
  - snapshots survive later catalog edits
  - prices are stored in COP integer minor units or decimal COP consistently across catalog and snapshot tables

## API Plan

### Meta webhook surface on `n8n-test.joypaw.tech`
- n8n exposes the Meta `GET` verification and `POST` inbound webhook workflow.
- n8n validates `X-Hub-Signature-256` before parsing or forwarding a message.
- n8n deduplicates Meta message IDs and normalizes sender `wa_id` to E.164.
- n8n persists only redacted state needed for conversation replay and sends a sanitized, schema-validated command to `reservation-bot-test`.
- n8n holds Meta and OpenAI credentials in its encrypted credential store; it never holds the OpenResto MCP credential.

### Private customer channel API on `test-rest.joypaw.tech`
- `GET /api/private/channels/whatsapp/restaurants`
  - list active locations for the single tenant
- `GET /api/private/channels/whatsapp/restaurants/{restaurantId}/occasion-catalog`
  - returns add-ons visible to customers
- `GET /api/private/channels/whatsapp/reservations`
  - list reservations owned by the authenticated WhatsApp phone
- `GET /api/private/channels/whatsapp/reservations/{id}`
  - reservation detail owned by the authenticated WhatsApp phone
- `PATCH /api/private/channels/whatsapp/reservations/{id}`
  - only `date`, `time`, `seats`
- `POST /api/private/channels/whatsapp/reservations/{id}/cancel`
  - explicit confirmation token or session idempotency key required
- `POST /api/private/channels/whatsapp/reservations/{id}/handoff`
  - routes to human WhatsApp workflow

### Create/availability execution path
- Availability:
  - bot may call existing operator MCP availability tool or a thin OpenResto wrapper that reuses the same service path
- Create:
  - bot may use the create path backed by the existing booking domain rules, but the write must end with OpenResto stamping phone ownership, channel metadata, selected add-on snapshots, and idempotency evidence in the same transaction

### Admin catalog/config API
- `GET /api/admin/restaurants/{restaurantId}/occasion-catalog`
- `POST /api/admin/restaurants/{restaurantId}/occasion-catalog`
- `PUT /api/admin/restaurants/{restaurantId}/occasion-catalog/{itemId}`
- `DELETE /api/admin/restaurants/{restaurantId}/occasion-catalog/{itemId}`
- `PUT /api/admin/restaurants/{restaurantId}/whatsapp-config`
  - human handoff number
  - location visibility toggle for WhatsApp test flow

## Conversation Workflow Plan

### Happy path
1. Meta sends an inbound message to the n8n webhook.
2. n8n verifies HMAC, deduplicates the event, derives the verified sender phone, and persists redacted conversation state.
3. n8n invokes OpenAI through a strict structured-output schema with no tools.
4. n8n sends the schema-validated intent, trusted sender phone, session id, and idempotency key to `reservation-bot-test`.
5. Bot orchestrator validates the intent and classifies it:
   - availability
   - create
   - list
   - change
   - cancel
   - human handoff
6. n8n gathers missing structured fields and presents an explicit summary and confirmation prompt before create/change/cancel.
7. After confirmation, n8n invokes the constrained bot operation.
8. Bot executes fixed OpenResto/MCP operations; OpenResto applies idempotent write logic and phone ownership enforcement.
9. n8n formats and sends the WhatsApp response, while OpenResto records mutation audits.

### Human handoff
1. n8n detects an explicit handoff request, unsupported intent, or bot-service escalation response.
2. n8n calls the fixed OpenResto handoff operation to create a durable redacted audit record.
3. n8n sends/forwards the minimal sanitized summary to the configured human WhatsApp destination.
4. n8n replies that a human will continue on WhatsApp.

## Test Plan

### OpenResto backend tests

#### New migration tests
- `OpenRestoApi.Tests/Migrations/WhatsAppCustomerChannelMigrationTests.cs`
- `OpenRestoApi.Tests/Migrations/OccasionCatalogSnapshotMigrationTests.cs`

#### New service tests
- `OpenRestoApi.Tests/Services/WhatsAppReservationChannelServiceTests.cs`
- `OpenRestoApi.Tests/Services/WhatsAppPhoneOwnershipServiceTests.cs`
- `OpenRestoApi.Tests/Services/ChannelIdempotencyServiceTests.cs`
- `OpenRestoApi.Tests/Services/OccasionCatalogServiceTests.cs`
- `OpenRestoApi.Tests/Services/WhatsAppHandoffServiceTests.cs`

#### New integration tests
- `OpenRestoApi.Tests/Integration/WhatsAppCustomerReservationFlowIntegrationTests.cs`
- `OpenRestoApi.Tests/Integration/WhatsAppCustomerOwnershipIntegrationTests.cs`
- `OpenRestoApi.Tests/Integration/AdminOccasionCatalogControllerTests.cs`
- `OpenRestoApi.Tests/Integration/WhatsAppHandoffIntegrationTests.cs`

#### Existing tests to update/extend
- `OpenRestoApi.Tests/Integration/OperatorMcpIntegrationTests.cs`
  - prove availability/create reuse does not leak list/update/cancel behavior into operator ownership
- `OpenRestoApi.Tests/Integration/OperatorReservationOwnershipIntegrationTests.cs`
  - guard against regressions in operator ownership
- `OpenRestoApi.Tests/Infrastructure/NotificationWorkerTests.cs`
  - add human handoff work item coverage if handoff piggybacks on the queue

### Bot service tests
- `OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`
- `OpenRestoReservationBot.Tests/ConversationOrchestratorTests.cs`
- `OpenRestoReservationBot.Tests/ConfirmationStateMachineTests.cs`
- `OpenRestoReservationBot.Tests/OpenAiChatProviderTests.cs`
- `OpenRestoReservationBot.Tests/OpenRestoChannelClientTests.cs`
- `OpenRestoReservationBot.Tests/OpenRestoOperatorMcpClientTests.cs`
- `OpenRestoReservationBot.Tests/RedactionTests.cs`

### Frontend tests
- `openresto-frontend/tests/api/admin.whatsapp-catalog.test.ts`
- `openresto-frontend/tests/components/admin/settings/OccasionCatalogCard.test.tsx`
- `openresto-frontend/tests/components/admin/settings/HandoffWhatsAppCard.test.tsx`
- `openresto-frontend/tests/app/admin/settings.test.tsx`

### E2E / environment tests
- `openresto-frontend/e2e/admin-whatsapp-catalog.spec.ts`
- `tests/e2e/whatsapp-test-foundation.md`
  - scripted manual/E2E checklist for Meta sandbox, bot service, and n8n handoff

## Docker And Environment Plan

### `docker-compose.test-rest.yml`
- Keep existing `backend`, `frontend`, and `reverse-proxy`.
- Add `reservation-bot` service:
  - built from `OpenRestoReservationBot/Dockerfile`
  - private network access to `backend`
  - environment variables for OpenAI provider config, bot-to-OpenResto credential, and redaction settings
- Add `n8n` service:
  - isolated persistent volume
  - internal network access to `backend` and `reservation-bot`
  - no public write path to booking APIs except through explicit credentials

### `traefik/test-rest.yml`
- Keep existing router for `test-rest.joypaw.tech`.
- Add host routers:
  - `reservation-bot-test.joypaw.tech` -> `reservation-bot`
  - `n8n-test.joypaw.tech` -> `n8n`
- Apply HTTPS-only routing and terminate the public Meta webhook path at n8n-test.joypaw.tech; keep bot and OpenResto service endpoints private or authenticated.

### Environment variables to add to examples only
- OpenResto:
  - `META_WHATSAPP_APP_SECRET`
  - `META_WHATSAPP_VERIFY_TOKEN`
  - `META_WHATSAPP_PHONE_NUMBER_ID`
  - `WHATSAPP_CHANNEL_SHARED_SECRET`
- Reservation bot:
  - `OPENRESTO_BASE_URL`
  - `OPENRESTO_CHANNEL_API_KEY`
  - `OPENAI_API_KEY`
  - `OPENAI_MODEL`
  - `BOT_PROVIDER`
- n8n:
  - `N8N_BASIC_AUTH_USER`
  - `N8N_BASIC_AUTH_PASSWORD`
  - handoff webhook credentials as secret env vars

## Verification Gates
- Fresh database migrate-up passes with both new migrations.
- Upgrade migrate-up from the current develop schema passes.
- Meta webhook signature mismatch returns unauthorized and writes no business mutation.
- Replayed webhook events are deduplicated.
- Customer list/change/cancel calls cannot cross phone ownership boundaries.
- Customer update rejects name/email/location/notes changes.
- Booking snapshots preserve original add-on names/prices after catalog edits.
- Bot cannot execute create/change/cancel without explicit confirmation state.
- Logs and replay payloads store redacted phone/email/token values.
- Test-rest Compose stack exposes all three hostnames correctly in the isolated environment.

## Primary Risks To Manage During Execution
- Reusing operator MCP create without re-stamping phone ownership could silently create the wrong security model.
- Meta status events and user messages can arrive out of order; idempotent event handling must key on provider identifiers, not arrival sequence.
- Free-form notes and LLM prompts are the most likely PII leakage path; redaction needs dedicated tests, not just coding conventions.
- n8n can become an accidental write authority if workflow credentials are over-scoped; all booking writes must remain OpenResto-owned.
