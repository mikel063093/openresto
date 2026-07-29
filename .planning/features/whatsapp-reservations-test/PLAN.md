# WhatsApp Reservations Test Plan

## Goal
Execute the remaining work to deliver the WhatsApp Business reservation channel in test only, using the current branch's OpenResto-side foundation as baseline and completing the missing create, bot, n8n, admin UI, deployment, runbook, and verification slices.

## Planning Rules
- Planner-only artifact. No code, deploy, push, secret creation, or infra mutation is performed here.
- Treat the branch's current OpenResto WhatsApp backend work as partially complete, not as finished feature delivery.
- Preserve the locked decision chain:
  - `n8n` validates Meta and is the only assertion issuer
  - OpenResto is booking authority
  - private API stays blocked from the public proxy
- All user-facing and operator-facing copy added by execution must be `es-CO`.
- All execution should be TDD-first and phase-local enough for `$gsd-execute-phase`.

## Dependency Graph
- Phase 1 depends on current branch baseline only.
- Phase 2 depends on Phase 1.
- Phase 3 depends on Phase 1 for auth shape and Phase 2 for create contract.
- Phase 4 depends on Phases 2 and 3.
- Phase 5 depends on Phase 2 for admin APIs and may run partly in parallel with Phase 4 once contracts are frozen.
- Phase 6 depends on Phases 3, 4, and 5.
- Phase 7 depends on Phase 6.
- Phase 8 depends on all prior phases.

## Level-C Execution Phases

### Phase 1. Reconcile OpenResto WhatsApp authority baseline

#### Execution status
- Completed on Wednesday, July 29, 2026.
- Acceptance criteria satisfied in the current worktree.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Close the remaining OpenResto authority gaps so the backend owns every write-critical rule before any bot/n8n orchestration is added.

#### Dependencies
- Current branch backend baseline only.

#### Exact code areas
- `OpenRestoApi/Core/Application/Services/BookingService.cs`
- `OpenRestoApi/Core/Application/Services/WhatsAppReservationChannelService.cs`
- `OpenRestoApi/Core/Application/Services/OccasionCatalogService.cs`
- `OpenRestoApi/Core/Application/Services/WhatsAppPhoneOwnershipService.cs`
- `OpenRestoApi/Core/Application/Services/ChannelIdempotencyService.cs`
- `OpenRestoApi/Core/Application/DTOs/WhatsAppChannelDtos.cs`
- `OpenRestoApi/Core/Application/DTOs/BookingDto.cs`
- `OpenRestoApi/Controllers/WhatsAppChannelReservationsController.cs`
- `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`
- `OpenRestoApi/Migrations/*`
- `OpenRestoApi.Tests/Services/*`
- `OpenRestoApi.Tests/Integration/WhatsAppChannelReservationsIntegrationTests.cs`
- `OpenRestoApi.Tests/Migrations/*`

#### Work
1. Freeze the current OpenResto private channel contract and document current implemented endpoints as baseline.
2. Add a new atomic create endpoint under the private WhatsApp API.
3. Ensure create path performs availability validation and reservation write in one OpenResto authority path.
4. Require customer email on create.
5. Stamp trusted WhatsApp ownership using only assertion-derived phone.
6. Persist immutable extras snapshots as part of the same create transaction.
7. Extend idempotency handling to cover create, not just update/cancel.
8. Ensure stale-writer/concurrency semantics are explicit for change/cancel and not bypassed by replays.
9. Add durable handoff audit persistence and API surface.
10. Add restaurant-level WhatsApp settings persistence if not already present.

#### Database/migration changes
- Add or extend tables/columns for:
  - restaurant WhatsApp settings
  - handoff audit record
  - channel session/event references only if OpenResto owns any of them
- Keep existing migrations additive.
- Add migration tests for fresh install and upgrade symmetry.

#### Acceptance criteria
- OpenResto exposes a single atomic create operation for the WhatsApp channel.
- Create fails if email is absent.
- Create replays same result on same idempotency key + fingerprint.
- Create rejects same key with different fingerprint.
- Extras snapshots are immutable after create.
- Handoff audit is durable and queryable for support/debugging.

#### TDD test matrix
- Unit
  - required email validation
  - ownership normalization
  - snapshot creation idempotency
  - create fingerprint computation
- Integration
  - create success
  - create duplicate replay
  - create changed fingerprint conflict
  - create capacity race conflict
  - create denied for archived/invisible restaurant
  - create denied when WhatsApp test toggle disabled
  - handoff audit persisted
- Migration
  - fresh install creates new WhatsApp settings and handoff schema
  - upgrade schema matches fresh install

#### Rollback boundary
- Revert only WhatsApp-specific migrations and private controller/service wiring for this phase.
- Do not roll back previously merged operator MCP work or unrelated booking logic.

#### Deployment boundary
- None. Local/container verification only.

### Phase 2. Lock the reservation-bot internal service contract

#### Execution status
- Completed on Wednesday, July 29, 2026.
- Acceptance criteria satisfied in the current worktree.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Add a dedicated internal bot service with a fixed operation contract that cannot expand into arbitrary tool execution.

#### Dependencies
- Phase 1 completed.

#### Exact folders/contracts
- `OpenRestoReservationBot/OpenRestoReservationBot.csproj`
- `OpenRestoReservationBot/Program.cs`
- `OpenRestoReservationBot/appsettings*.json`
- `OpenRestoReservationBot/Contracts/BotOperationRequest.cs`
- `OpenRestoReservationBot/Contracts/BotOperationResponse.cs`
- `OpenRestoReservationBot/Contracts/OpenRestoWhatsAppDtos.cs`
- `OpenRestoReservationBot/Controllers/BotOperationsController.cs`
- `OpenRestoReservationBot/Services/BotOperationRouter.cs`
- `OpenRestoReservationBot/Services/OpenRestoPrivateChannelClient.cs`
- `OpenRestoReservationBot/Services/OpenRestoAvailabilityClient.cs`
- `OpenRestoReservationBot/Security/*`
- `OpenRestoReservationBot.Tests/*`
- `openresto.sln`

#### Work
1. Create a separate ASP.NET Core service for `reservation-bot-test`.
2. Define a small request/response contract for the eight allowed operations: availability, create, list, detail, update, cancel, occasion catalog, and handoff.
3. Add request authentication between n8n and bot.
4. Forward the n8n-issued assertion and correlation metadata unchanged to OpenResto.
5. Keep provider abstraction separate from tool execution logic.
6. Ensure bot never stores or receives Meta/OpenAI/MCP secrets.
7. Ensure bot never accepts arbitrary route names or raw tool names.

#### Trust boundaries
- n8n is caller and issuer.
- Bot is policy-constrained translator/orchestrator only.
- OpenResto is authority for reads/writes.

#### Acceptance criteria
- Bot accepts only fixed operation names.
- Bot can call OpenResto availability and private reservation APIs through typed clients only.
- Bot does not contain Meta webhook handlers.
- Bot does not contain OpenAI credential use.
- Bot cannot mint assertions.

#### TDD test matrix
- Controller auth deny/allow tests.
- Contract validation tests for unknown operation names.
- Routing tests for each supported operation.
- Tests proving assertion forwarding headers are preserved.
- Tests proving arbitrary endpoint/tool injection is rejected.

#### Rollback boundary
- Remove bot project and solution wiring only.

#### Deployment boundary
- None. Local service and test harness only.

### Phase 3. Add durable n8n test stack and state storage

#### Execution status
- Completed on Wednesday, July 29, 2026.
- Acceptance criteria satisfied in the current worktree.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Create the test-only n8n stack and durable storage layout needed for Meta webhook handling, session state, dedupe, ordering, and replay-safe observability.

#### Dependencies
- Phase 1 and contract assumptions from Phase 2.

#### Exact folders/contracts
- `n8n/test/README.md`
- `n8n/test/docker-compose.n8n-test.yml` or test service additions in `docker-compose.test-rest.yml`
- `n8n/test/workflows/`
- `n8n/test/credentials/README.md`
- `n8n/test/storage/README.md`
- `.env.example`
- `docs/runbooks/` or feature-local runbook references

#### Work
1. Decide and record durable state backend for n8n test.
2. Define internal-only networking where n8n reaches only the bot, the bot bridges to OpenResto, and no public Traefik/DNS route is introduced in Phase 3.
3. Define stored artifacts:
  - session state
  - dedupe ledger
  - ordering watermark
  - sanitized replay payload
  - outbound idempotency correlation
4. Define container volumes and backup/cleanup expectations for test.
5. Define secret injection points without putting real values in repo.

#### Trust boundaries
- Meta and OpenAI credentials live only in n8n secret store.
- n8n may call bot, but not arbitrary private OpenResto routes or the backend network directly.
- Public topology, DNS, and external webhook exposure are deferred to Phase 6.

#### Acceptance criteria
- n8n stack definition is durable across restart.
- Required state categories are persisted.
- Secrets placeholders are documented but no real secrets are stored in repo.
- Phase 3 adds no public n8n or bot route and no shared n8n/backend network.

#### TDD test matrix
- Compose/config validation checks.
- Workflow import validation.
- Storage boot/restart smoke.
- Replay/dedupe persistence smoke.

#### Rollback boundary
- Remove n8n test-only definitions and docs only.

#### Deployment boundary
- Test-only compose and docs. No live deploy, public DNS, or Traefik exposure in this phase.

### Phase 4. Implement n8n workflows for Meta, LLM, confirmations, and handoff

#### Execution status
- Completed on Wednesday, July 29, 2026.
- Acceptance criteria satisfied in the current worktree through versioned workflow exports, contract documentation, static contract tests, import validation, compose validation, and targeted secret scanning.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Define the complete orchestration layer in n8n for inbound Meta events through outbound customer replies and human handoff.

#### Dependencies
- Phase 2 bot contract.
- Phase 3 durable stack.

#### Exact folders/contracts
- `n8n/test/workflows/whatsapp-meta-verification.json`
- `n8n/test/workflows/whatsapp-inbound-router.json`
- `n8n/test/workflows/whatsapp-confirmation-state-machine.json`
- `n8n/test/workflows/whatsapp-handoff.json`
- `n8n/test/workflows/whatsapp-observability.json`
- `n8n/test/workflows/whatsapp-template-messages.json`
- `n8n/test/docs/contract.md`

#### Work
1. Add Meta `GET` verification workflow.
2. Add Meta `POST` inbound webhook workflow with HMAC verification before parse/dispatch.
3. Implement event ordering and dedupe using durable storage.
4. Add structured-output LLM step with provider abstraction and no tools.
5. Implement confirmation state machine for create/change/cancel.
6. Add `es-CO` response/template set.
7. Add PII redaction before logs/replay storage.
8. Add unsupported-intent guardrails and human handoff path.
9. Mint the OpenResto assertion only after Meta verification and trusted sender normalization.
10. Include `kid` in assertion issuance and document dual-key rotation flow.

#### Acceptance criteria
- n8n rejects invalid Meta signatures before bot call.
- Out-of-order or duplicate Meta events do not double-execute.
- Mutating intents require explicit confirmation.
- LLM outputs are schema-constrained.
- All customer/ops copy is `es-CO`.
- Handoff path produces sanitized summary only.

#### TDD test matrix
- Workflow-level mocked webhook tests:
  - GET verify success/failure
  - invalid HMAC rejection
  - duplicate event suppression
  - out-of-order event ignored or reconciled
  - structured output schema reject/retry
  - confirmation required for create/change/cancel
  - handoff triggered by unsupported intent
- Security tests:
  - no secrets in workflow logs/export
  - PII redaction in stored replay artifacts

#### Rollback boundary
- Remove only n8n workflows and bot-facing orchestration docs.

#### Deployment boundary
- Test-only workflow artifacts. No public enablement outside test domains.

### Phase 5. Add admin backend and Expo UI for WhatsApp settings

#### Execution status
- Completed on Wednesday, July 29, 2026.
- Acceptance criteria satisfied in the current worktree.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Expose SuperAdmin-managed catalog and WhatsApp/handoff controls through the existing admin stack.

#### Dependencies
- Phase 1 for persistence and API surface.

#### Exact code areas
- `OpenRestoApi/Controllers/AdminOccasionCatalogController.cs`
- `OpenRestoApi/Controllers/AdminRestaurantWhatsAppSettingsController.cs`
- `OpenRestoApi/Core/Application/DTOs/RestaurantOccasionCatalogDtos.cs`
- `OpenRestoApi/Core/Application/DTOs/RestaurantWhatsAppSettingsDtos.cs`
- `OpenRestoApi/Core/Application/Services/OccasionCatalogService.cs`
- `OpenRestoApi/Core/Application/Services/RestaurantWhatsAppSettingsService.cs`
- `openresto-frontend/app/admin/settings.tsx`
- `openresto-frontend/api/admin.ts`
- `openresto-frontend/components/admin/settings/OccasionCatalogCard.tsx`
- `openresto-frontend/components/admin/settings/OccasionCatalogRow.tsx`
- `openresto-frontend/components/admin/settings/HandoffWhatsAppCard.tsx`
- `openresto-frontend/components/admin/settings/WhatsAppTestSettingsCard.tsx`
- `openresto-frontend/tests/**/*`

#### Work
1. Add backend DTOs/services/controllers for restaurant WhatsApp settings.
2. Extend frontend admin API layer.
3. Add settings cards and CRUD rows in admin settings.
4. Keep visual language consistent with current settings surface.
5. Use `es-CO` labels and validation messages.
6. Ensure only SuperAdmin can manage this configuration.

#### Acceptance criteria
- SuperAdmin can manage catalog items, test visibility toggle, and handoff number per restaurant.
- BookingEditor/Viewer cannot modify these settings.
- UI and API validations are localized in `es-CO`.

#### TDD test matrix
- Backend authorization/integration tests.
- Frontend component tests.
- Frontend API tests.
- Typecheck/lint tests on changed files.

#### Rollback boundary
- Remove WhatsApp admin controllers/services/UI only.

#### Deployment boundary
- None. Local UI/API verification only.

### Phase 6. Define test-only deployment topology, network boundaries, and secrets injection

#### Execution status
- Completed locally on Wednesday, July 29, 2026.
- Repository topology and placeholder-only secret-injection contract were independently revalidated; this is not a deployment.
- Verification captured in `.planning/features/whatsapp-reservations-test/VERIFICATION.md`.

#### Objective
Add the deployment-ready but test-only topology definition for `n8n-test` and `reservation-bot-test`.

#### Dependencies
- Phases 2 through 5.

#### Exact files
- `docker-compose.test-rest.yml`
- `traefik/test-rest.yml`
- `nginx/default.conf.template`
- `nginx-vps/default.conf.template` only if test routing documentation needs parity notes
- `.env.example`
- `n8n/test/README.md`
- `OpenRestoReservationBot/Dockerfile`
- `OpenRestoReservationBot/appsettings.json`

#### Work
1. Add bot service and n8n service definitions to the test compose topology.
2. Define internal/private network connectivity:
  - public: Traefik to `n8n-test` and `reservation-bot-test`
  - private: bot to OpenResto backend private route
3. Preserve the `404` block on `/api/private/channels/whatsapp/` at the public proxy.
4. Define DNS/router labels for:
  - `n8n-test.joypaw.tech`
  - `reservation-bot-test.joypaw.tech`
5. Document all required env var names and where each secret is injected.
6. Keep production compose/routers untouched.

#### Secrets contract
- Real values are `USER/AWAITING`.
- Repo may contain names only:
  - `META_VERIFY_TOKEN`
  - `META_APP_SECRET`
  - `META_ACCESS_TOKEN`
  - `OPENAI_API_KEY`
  - `WHATSAPP_CHANNEL_INTERNAL_CALLER_CREDENTIAL`
  - `WHATSAPP_ASSERTION_ACTIVE_KID`
  - `WHATSAPP_ASSERTION_ACTIVE_KEY`
  - `WHATSAPP_ASSERTION_PREVIOUS_KEY`
  - bot-to-openresto internal credential if separate

#### Acceptance criteria
- Test topology clearly separates public and private boundaries.
- No production compose or production Traefik path is modified.
- Secrets placement is documented with placeholders only.

#### TDD test matrix
- Compose config validation.
- Route exposure smoke.
- Nginx private-path denial regression.
- Container-to-container connectivity smoke.

#### Rollback boundary
- Revert only test compose/router/docs additions.

#### Deployment boundary
- Test-only checklist and dry-run validation only.

### Phase 7. Write Meta/WABA provisioning and operator runbook

#### Objective
Create the external prerequisite runbook and mark all user-owned steps explicitly.

#### Dependencies
- Phase 6 topology and env names.

#### Exact files
- `.planning/features/whatsapp-reservations-test/VERIFICATION.md`
- `n8n/test/README.md`
- `docs/runbooks/whatsapp-test-provisioning.md` or feature-local equivalent

#### Work
1. Document Meta/WABA prerequisites with `USER/AWAITING` markers.
2. Include:
  - Business Manager access
  - WhatsApp Business Account setup
  - test phone number registration
  - webhook callback URL
  - verification token placement
  - app secret placement
  - template approval dependencies if needed
3. Document DNS readiness checks for the three test domains.
4. Document rotation procedure for HMAC assertion keys with `kid`.
5. Document future asymmetric migration option as deferred design note.

#### Acceptance criteria
- Every external dependency is clearly marked `USER/AWAITING`.
- No false implication that the repo alone can complete Meta provisioning.

#### TDD test matrix
- Runbook review checklist only.

#### Rollback boundary
- Remove runbook/docs only.

#### Deployment boundary
- Documentation only.

### Phase 8. Verification, security review, E2E sandbox, and smoke gates

#### Objective
Close the feature with comprehensive automated and manual verification without any production promotion.

#### Dependencies
- All prior phases complete.

#### Exact verification artifacts
- `.planning/features/whatsapp-reservations-test/VERIFICATION.md`
- `OpenRestoApi.Tests/**/*`
- `OpenRestoReservationBot.Tests/**/*`
- `n8n/test/verification/*`
- optional `openresto-frontend/tests/**/*`

#### Work
1. Run backend unit/integration/migration suites.
2. Run bot unit/integration suites.
3. Validate n8n workflow import and mocked execution.
4. Run end-to-end sandbox against test domains or local compose equivalent.
5. Run security review focused on:
  - secrets exposure
  - assertion replay
  - key rotation
  - public/private routing
  - PII redaction
6. Capture smoke checklist and observability verification.
7. Confirm no production deployment or promotion step exists.

#### Acceptance criteria
- End-to-end sandbox completes:
  - availability
  - create
  - list
  - change
  - cancel
  - handoff
- No public route exposes the private WhatsApp API.
- Logs and replay artifacts are redacted.
- Verification artifact records exact commands and residual risks.

#### TDD test matrix
- Backend full suite.
- Bot full suite.
- Frontend targeted suite.
- Compose/router smoke.
- E2E happy path and failure-path scripts.
- Security checklist review.

#### Rollback boundary
- Revert the final phase’s test-only verification harness and docs only if needed.

#### Deployment boundary
- Test-only smoke and sandbox. Explicitly no production promotion.

## Wave Grouping For `$gsd-execute-phase`
- Wave 1
  - Phase 1
- Wave 2
  - Phase 2
  - Phase 5 backend-only sub-slice if Phase 1 settings schema is stable
- Wave 3
  - Phase 3
  - Phase 5 frontend/UI sub-slice
- Wave 4
  - Phase 4
  - Phase 6
- Wave 5
  - Phase 7
  - Phase 8

## Cross-Phase Risks And Controls
- Risk: create path diverges from existing booking logic.
  - Control: reuse `BookingService` validation path and test conflict parity.
- Risk: shared-secret assertion model creates excessive blast radius.
  - Control: require `kid`, dual-key rotation, short TTL, and a future asymmetric seam.
- Risk: n8n duplicates or reorders events.
  - Control: durable dedupe ledger and ordering watermark before mutation.
- Risk: private API leaks publicly.
  - Control: preserve Nginx/Traefik deny rules and add regression tests.
- Risk: admin UI adds inconsistent terminology.
  - Control: centralize `es-CO` copy and validate in tests/review.

## Definition Of Done
- All eight phases pass their acceptance criteria.
- Verification artifact records concrete commands, results, and residual risks.
- Feature remains test-only.
- No production deployment step exists in artifacts or implementation.
