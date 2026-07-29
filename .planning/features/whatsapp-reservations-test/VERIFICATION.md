# WhatsApp Reservations Test Verification

## Purpose
Define the mandatory verification gates for executing `.planning/features/whatsapp-reservations-test/PLAN.md`.

## Global Gates
- No production deploy, push, or secret creation occurs during this feature.
- Every mutation path is covered by automated tests before end-to-end smoke.
- All user-facing and operator-facing copy is validated in `es-CO`.
- Public/private routing boundary is verified on every relevant infra change.

## Command Buckets To Capture During Execution

### Backend
- `dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~WhatsApp|FullyQualifiedName~Occasion|FullyQualifiedName~ChannelIdempotency|FullyQualifiedName~BookingService"`
- `dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`

### Bot
- `dotnet test OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`

### Frontend
- `npm --prefix openresto-frontend test -- --runInBand`
- `npx --prefix openresto-frontend tsc --noEmit -p openresto-frontend/tsconfig.json`

### Compose/infra
- `docker compose -f docker-compose.test-rest.yml config`
- test-only compose up/down and route smoke commands to be filled during execution

### n8n workflow verification
- workflow import/validation commands or documented UI export checks to be captured during execution

## TDD Matrix

### OpenResto create authority
- RED
  - create fails without email
  - create fails when confirmation missing
  - create rejects extras from another restaurant
  - create rejects duplicate key with changed fingerprint
  - create replays same response on identical retry
  - create stamps normalized phone from trusted assertion only
  - create denies archived/invisible/disabled restaurant
  - create writes immutable extras snapshots
- GREEN
  - exact passing tests recorded with command output summary

### OpenResto list/change/cancel/handoff
- list/get deny foreign phone ownership
- update only allows date/time/seats
- update rejects stale concurrency token
- cancel is idempotent and blocks further mutation
- handoff writes durable audit and sanitized summary reference

### Migration verification
- fresh install contains:
  - booking phone ownership columns
  - occasion catalog
  - extras snapshots
  - idempotency records
  - WhatsApp settings
  - handoff audit
- upgrade schema matches fresh install schema

### Assertion security
- missing assertion rejected
- wrong issuer rejected
- wrong audience rejected
- wrong scope rejected
- wrong action rejected
- expired assertion rejected
- replayed `jti` rejected
- previous-key verification passes only inside overlap window
- removed previous key causes old `kid` rejection after rotation cutover

### Bot contract verification
- unknown operation rejected
- arbitrary route/tool injection rejected
- assertion/correlation forwarding preserved
- Meta/OpenAI credentials absent from bot runtime config and logs

### n8n workflow verification
- Meta `GET` verify success/fail
- invalid `X-Hub-Signature-256` rejected
- duplicate inbound message suppressed
- out-of-order event does not double-execute
- confirmation gate required for create/change/cancel
- structured output schema reject/retry path works
- human handoff path produces `es-CO` customer reply and sanitized operator summary

### Admin UI/API verification
- SuperAdmin can manage catalog and WhatsApp settings
- non-SuperAdmin denied
- `es-CO` labels/messages render correctly
- frontend tests and typecheck pass on changed files

### Route and deployment boundary verification
- public `test-rest` proxy returns `404` for `/api/private/channels/whatsapp/*`
- internal bot/OpenResto connectivity works on test network
- Phase 3 keeps `n8n-test` and `reservation-bot-test` off the public edge; any public route belongs to Phase 6
- no production compose/router files changed as part of this feature

## E2E Sandbox Matrix
- Happy path
  - customer asks for availability
  - customer receives options in `es-CO`
  - customer creates reservation with email and extras
  - OpenResto persists phone ownership and extras snapshots
- Change path
  - customer lists own reservation
  - customer confirms seat/date/time change
  - stale concurrency attempt is rejected safely
- Cancel path
  - customer confirms cancellation
  - duplicate cancel replay returns same result
- Handoff path
  - unsupported request escalates to human WhatsApp
  - audit exists and customer receives `es-CO` handoff acknowledgement
- Failure path
  - invalid Meta signature
  - replayed event
  - invalid assertion
  - fingerprint mismatch on idempotency replay

## Security Review Checklist
- No real secrets committed to repo.
- No secret appears in structured logs, exported workflows, or replay artifacts.
- Assertion rotation procedure is documented and testable.
- Shared-secret blast radius is documented; asymmetric migration seam exists.
- Bot cannot mint assertions or call arbitrary OpenResto routes.
- OpenResto remains final authority for create/change/cancel writes.
- Public proxy still blocks private WhatsApp API.

## Observability Checklist
- Correlation id flows from Meta event to n8n to bot to OpenResto.
- Audit trail exists for create/change/cancel/handoff.
- Replay/dedupe ledger is queryable in test.
- Redacted payload capture exists for debugging without leaking full PII.

## USER/AWAITING External Checks
- Meta app, WABA, test number, webhook registration, and template approvals.
- DNS availability for:
  - `test-rest.joypaw.tech`
- Public `n8n-test.joypaw.tech` and `reservation-bot-test.joypaw.tech` readiness is deferred until Phase 6
- Real secret injection into test environment.

## Exit Criteria
- All phase acceptance criteria in the plan are satisfied.
- Verification commands and summarized results are recorded here during execution.
- Residual risks are explicitly listed.
- Final verification statement confirms: test-only complete, no production promotion performed.

## Execution Results

### Phase 1 status
- Phase 1 completed in the current worktree on Wednesday, July 29, 2026.
- Scope executed: OpenResto authority baseline only.
- No production deploy, push, secret creation, or infra mutation performed.

### Phase 2 status
- Phase 2 completed in the current worktree on Wednesday, July 29, 2026.
- Scope executed: reservation-bot internal contract only.
- No production deploy, push, secret creation, or infra mutation performed.

### Phase 3 status
- Phase 3 completed in the current worktree on Wednesday, July 29, 2026.
- Scope executed: durable n8n test stack and state storage foundation only.
- Remediation applied on Wednesday, July 29, 2026 to remove premature public topology and enforce network segmentation.
- No production deploy, push, secret creation, DNS mutation, public Traefik exposure, or Phase 4 workflows performed.

### Phase 4 status
- Phase 4 completed in the current worktree on Wednesday, July 29, 2026.
- Scope executed: versioned n8n workflow exports, workflow contract documentation, static workflow security tests, n8n import validation, compose validation, and targeted Phase 4 secret scanning.
- No production deploy, push, secret creation, DNS mutation, public Traefik exposure, or Phase 5+ implementation performed.

### Phase 5 status
- Phase 5 completed in the current worktree on Wednesday, July 29, 2026.
- Scope executed: SuperAdmin-only admin backend for WhatsApp settings, Expo settings cards for WhatsApp visibility/handoff/catalog management, typed admin API wiring, and focused backend/frontend verification.
- No deployment, DNS mutation, push, secret creation, Phase 6 topology work, or Phase 7+ workflow execution performed.

### Commands run on Wednesday, July 29, 2026
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~WhatsAppChannelReservationsIntegrationTests|FullyQualifiedName~WhatsAppAuthorityBaselineMigrationTests|FullyQualifiedName~OpenApiDocumentationTests"`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet sln openresto.sln add OpenRestoReservationBot/OpenRestoReservationBot.csproj OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~TestRestN8nStackTopologyTests"`
- `export CORS_ORIGINS='<temporal-redacted>' JWT_KEY='<temporal-redacted>' ADMIN_EMAIL='<temporal-redacted>' ADMIN_PASSWORD='<temporal-redacted>' N8N_TEST_POSTGRES_PASSWORD='<temporal-redacted>' N8N_TEST_ENCRYPTION_KEY='<temporal-redacted>' N8N_TEST_BASIC_AUTH_USER='<temporal-redacted>' N8N_TEST_BASIC_AUTH_PASSWORD='<temporal-redacted>' ReservationBot__InternalCredential='<temporal-redacted>' WhatsAppChannel__InternalCallerCredential='<temporal-redacted>'; docker compose -f docker-compose.test-rest.yml config`
- `export CORS_ORIGINS='<temporal-redacted>' JWT_KEY='<temporal-redacted>' ADMIN_EMAIL='<temporal-redacted>' ADMIN_PASSWORD='<temporal-redacted>' N8N_TEST_POSTGRES_PASSWORD='<temporal-redacted>' N8N_TEST_ENCRYPTION_KEY='<temporal-redacted>' N8N_TEST_BASIC_AUTH_USER='<temporal-redacted>' N8N_TEST_BASIC_AUTH_PASSWORD='<temporal-redacted>' ReservationBot__InternalCredential='<temporal-redacted>' WhatsAppChannel__InternalCallerCredential='<temporal-redacted>'; docker compose -f docker-compose.test-rest.yml up -d --build backend reservation-bot-test n8n-test-postgres n8n-test`
- `docker exec test-rest-n8n-test-1 sh -lc "mkdir -p /data/channel-state/sessions /data/channel-state/dedupe /data/channel-state/ordering /data/channel-state/replay /data/channel-state/outbound && printf 'ok' > /data/channel-state/sessions/restart-marker.txt && printf 'ok' > /data/channel-state/dedupe/restart-marker.txt && printf 'ok' > /data/channel-state/ordering/restart-marker.txt && printf 'ok' > /data/channel-state/replay/restart-marker.txt && printf 'ok' > /data/channel-state/outbound/restart-marker.txt"`
- `docker restart test-rest-n8n-test-1`
- `docker exec test-rest-n8n-test-1 sh -lc "test -f /data/channel-state/sessions/restart-marker.txt && test -f /data/channel-state/dedupe/restart-marker.txt && test -f /data/channel-state/ordering/restart-marker.txt && test -f /data/channel-state/replay/restart-marker.txt && test -f /data/channel-state/outbound/restart-marker.txt && echo persisted"`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~TestRestN8nStackTopologyTests"`
- `docker exec test-rest-reservation-bot-test-1 sh -lc "curl -fsS http://test-rest-backend:8080/api/health >/dev/null"`
- `export CORS_ORIGINS='<temporal-redacted>' JWT_KEY='<temporal-redacted>' ADMIN_EMAIL='<temporal-redacted>' ADMIN_PASSWORD='<temporal-redacted>' N8N_TEST_POSTGRES_PASSWORD='<temporal-redacted>' N8N_TEST_ENCRYPTION_KEY='<temporal-redacted>' N8N_TEST_BASIC_AUTH_USER='<temporal-redacted>' N8N_TEST_BASIC_AUTH_PASSWORD='<temporal-redacted>' ReservationBot__InternalCredential='<temporal-redacted>' WhatsAppChannel__InternalCallerCredential='<temporal-redacted>'; docker compose -f docker-compose.test-rest.yml ps`
- `docker compose -f docker-compose.test-rest.yml down`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/repo zricethezav/gitleaks:latest dir /repo --no-banner --redact`
- `node scripts/generate-phase4-n8n-workflows.cjs`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/repo -e N8N_ENCRYPTION_KEY='<temporal-redacted>' -e DB_TYPE=sqlite -e N8N_USER_FOLDER=/tmp/n8n docker.n8n.io/n8nio/n8n:latest import:workflow --separate --input=/repo/n8n/test/workflows`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter FullyQualifiedName~N8nPhase4WorkflowContractTests`
- `export CORS_ORIGINS='<temporal-redacted>' JWT_KEY='<temporal-redacted>' ADMIN_EMAIL='<temporal-redacted>' ADMIN_PASSWORD='<temporal-redacted>' N8N_TEST_POSTGRES_PASSWORD='<temporal-redacted>' N8N_TEST_ENCRYPTION_KEY='<temporal-redacted>' N8N_TEST_BASIC_AUTH_USER='<temporal-redacted>' N8N_TEST_BASIC_AUTH_PASSWORD='<temporal-redacted>' ReservationBot__InternalCredential='<temporal-redacted>' WhatsAppChannel__InternalCallerCredential='<temporal-redacted>'; docker compose -f docker-compose.test-rest.yml config`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/repo zricethezav/gitleaks:latest dir /repo/n8n --no-banner --redact`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~AdminRestaurantWhatsAppSettingsControllerTests|FullyQualifiedName~RestaurantWhatsAppSettingsServiceTests|FullyQualifiedName~AdminOccasionCatalogControllerTests|FullyQualifiedName~RoleAuthorizationTests"`
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npm install`
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npm test -- --runInBand tests/components/admin/settings/WhatsAppTestSettingsCard.test.tsx tests/components/admin/settings/HandoffWhatsAppCard.test.tsx tests/components/admin/settings/OccasionCatalogCard.test.tsx tests/app/admin/settings.test.tsx tests/api/admin.test.ts`
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npx tsc --noEmit -p tsconfig.json`
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npx oxlint components/admin/settings/WhatsAppTestSettingsCard.tsx components/admin/settings/HandoffWhatsAppCard.tsx components/admin/settings/OccasionCatalogCard.tsx components/admin/settings/OccasionCatalogRow.tsx tests/components/admin/settings/WhatsAppTestSettingsCard.test.tsx tests/components/admin/settings/HandoffWhatsAppCard.test.tsx tests/components/admin/settings/OccasionCatalogCard.test.tsx app/admin/settings.tsx api/admin.ts tests/app/admin/settings.test.tsx tests/api/admin.test.ts`
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npx prettier --check api/admin.ts app/admin/settings.tsx components/admin/settings/WhatsAppTestSettingsCard.tsx components/admin/settings/HandoffWhatsAppCard.tsx components/admin/settings/OccasionCatalogCard.tsx components/admin/settings/OccasionCatalogRow.tsx tests/app/admin/settings.test.tsx tests/api/admin.test.ts tests/components/admin/settings/WhatsAppTestSettingsCard.test.tsx tests/components/admin/settings/HandoffWhatsAppCard.test.tsx tests/components/admin/settings/OccasionCatalogCard.test.tsx`

### Command summaries
- Focused backend suite: passed, `18` passed, `0` failed, `0` skipped, duration `7 s`.
- Full backend suite: passed, `1271` passed, `0` failed, `0` skipped, duration `1 m 17 s`.
- Focused bot suite: passed, `19` passed, `0` failed, `0` skipped, duration `1 s`.
- Full solution suite: passed. `OpenRestoReservationBot.Tests`: `19` passed, `0` failed, `0` skipped. `OpenRestoApi.Tests`: `1281` passed, `0` failed, `0` skipped, duration `1 m 23 s` for the backend project inside solution execution.
- Focused infra topology suite: passed, `6` passed, `0` failed, `0` skipped, duration `30 ms`.
- Remediation topology suite: passed in the current worktree, `6` passed, `0` failed, `0` skipped, duration `21 ms`, executed in `mcr.microsoft.com/dotnet/sdk:10.0` because `dotnet` is not installed on the host shell.
- Test compose config validation: passed after adding `n8n-test-volume-init`; the rendered config includes `reservation-bot-test`, `n8n-test-postgres`, `n8n-test-volume-init`, `n8n-test`, the internal-only bot network boundary, and the durable named volumes.
- Test stack boot and restart smoke: passed. `backend`, `reservation-bot-test`, `n8n-test-postgres`, and `n8n-test` all reached `healthy`; marker files written to sessions/dedupe/ordering/replay/outbound remained present after restarting `n8n-test`.
- Internal connectivity smoke: original Phase 3 note superseded by remediation. The intended boundary is `n8n-test -> reservation-bot-test -> test-rest-backend`; direct `n8n-test -> test-rest-backend` reachability is no longer allowed by topology.
- Static secret scan: `gitleaks` completed and reported `2` findings, both existing redacted frontend test fixtures:
  - `openresto-frontend/tests/components/admin/settings/OperatorCredentialsCard.test.tsx`
  - `openresto-frontend/tests/api/admin.test.ts`
  - No new Phase 3 secret exposure was identified after sanitizing verification artifacts and keeping repo placeholders non-secret.
- Current remediation static scan: passed by targeted source/rendered-config search. No `n8n-test.joypaw.tech`, `reservation-bot-test.joypaw.tech`, `WEBHOOK_URL`, or `N8N_EDITOR_BASE_URL` entries remain in Phase 3 compose, Traefik, topology tests, or the rendered compose output; the rendered config shows separate `test-rest-app-internal` and `test-rest-bot-internal` networks.
- Phase 4 workflow generation: passed. `scripts/generate-phase4-n8n-workflows.cjs` emitted `6` versioned workflow exports into `n8n/test/workflows/`.
- Phase 4 workflow import validation: passed. The official `docker.n8n.io/n8nio/n8n:latest` image imported all `6` workflow JSON files successfully into a disposable SQLite-backed n8n user folder.
- Phase 4 static contract suite: passed, `5` passed, `0` failed, `0` skipped, duration `25 ms`, executed in `mcr.microsoft.com/dotnet/sdk:10.0` because `dotnet` is not installed on the host shell.
- Phase 4 compose config validation: passed. The rendered config still shows the same named durable volumes and no additional public topology or direct `n8n-test -> test-rest-backend` wiring.
- Phase 4 targeted secret scan: passed. `gitleaks` on `/repo/n8n` reported `0` findings.
- Phase 5 focused backend suite: passed, `15` passed, `0` failed, `0` skipped, duration `5 s`, executed in `mcr.microsoft.com/dotnet/sdk:10.0` because `dotnet` is not installed on the host shell.
- Phase 5 frontend dependency install: completed locally in `openresto-frontend` to materialize the repo-declared `jest`, `typescript`, `prettier`, and `oxlint` toolchain for verification.
- Phase 5 frontend Jest suite: passed, `5` test suites, `138` tests passed, `0` failed, duration `3.521 s`.
- Phase 5 frontend typecheck: passed with `npx tsc --noEmit -p tsconfig.json`.
- Phase 5 scoped frontend lint: passed with `npx oxlint` on the changed Phase 5 files.
- Phase 5 scoped frontend format check: passed with `npx prettier --check` on the changed Phase 5 files.

### Phase 1 criteria confirmed
- Atomic private WhatsApp create endpoint implemented with authority-side availability validation, idempotency, trusted phone ownership stamping, and immutable extras snapshot persistence.
- Create rejects missing email, missing confirmation, archived restaurants, disabled WhatsApp restaurants, foreign extras, and changed-fingerprint idempotency reuse.
- Signed single-use assertion verification remains issuer-scoped and now supports `kid`-based active/previous HMAC verification overlap without allowing arbitrary keys.
- Public/private route boundary remains protected by existing Nginx denial tests; no production routing files were changed.
- Durable restaurant WhatsApp settings and handoff audit persistence were added and verified through migration and integration tests.

### Phase 2 criteria confirmed
- `OpenRestoReservationBot` was added as a separate ASP.NET Core project with its own test project and included in `openresto.sln`.
- The bot contract is closed to the fixed operations `availability`, `create`, `list`, `detail`, `update`, `cancel`, `occasionCatalog`, and `handoff`.
- The bot rejects unknown operations, unexpected root fields, unexpected nested fields, and missing n8n assertion headers.
- n8n-to-bot authentication is enforced with a dedicated configured internal bearer credential that is separate from the OpenResto private channel credential.
- The bot forwards the n8n assertion string unchanged to the typed OpenResto private client and does not mint or parse assertion claims.
- OpenResto calls are bound to configuration-backed base URLs, fixed paths, typed request models, explicit timeouts, cancellation tokens, and safe correlation-id propagation.
- The bot does not include Meta webhook handlers, OpenAI SDK wiring, direct MCP operator client usage, or secrets placeholders for Meta/OpenAI/MCP credentials.
- Safe placeholders were added to `.env.example` and bot `appsettings*.json` without introducing real secrets.

### Phase 3 criteria confirmed
- `docker-compose.test-rest.yml` now defines a durable `n8n-test` stack with explicit Postgres state backend and named persistence volumes for session, dedupe, ordering, replay, outbound correlation, n8n data, and binary data.
- `reservation-bot-test` now bridges two private networks only: the n8n-side internal network and the backend-side internal network. `n8n-test` and `test-rest-backend` no longer share an internal network.
- `n8n-test` is not attached to `dokploy-network` and no longer carries public domain/editor/webhook configuration in Phase 3.
- `traefik/test-rest.yml` keeps only the existing `test-rest.joypaw.tech` route in this phase; public `n8n-test` and `reservation-bot-test` routing remains deferred to Phase 6.
- Placeholder-only secret injection points are documented in `.env.example`, `n8n/test/README.md`, `n8n/test/credentials/README.md`, and `n8n/test/storage/README.md`.
- The `n8n/test/workflows/` directory now contains importable Phase 4 workflow exports for Meta verification, inbound routing, confirmation state, handoff, observability, and `es-CO` template replies.

### Phase 4 criteria confirmed
- `n8n/test/workflows/whatsapp-meta-verification.json` responds to Meta `GET` verification with placeholder-only verify-token wiring.
- `n8n/test/workflows/whatsapp-inbound-router.json` validates `X-Hub-Signature-256` before parsing the inbound envelope, persists dedupe/order state under the durable volume paths, constrains the LLM to schema-shaped output with no tools, and calls only `POST /api/internal/reservation-bot/operations`.
- `n8n/test/workflows/whatsapp-confirmation-state-machine.json` enforces explicit confirmation for `create`, `update`, `cancel`, and handoff-adjacent mutation flows while persisting session state durably.
- `n8n/test/workflows/whatsapp-observability.json` redacts replay data before writing to `/data/channel-state/replay` and keeps outbound correlation in `/data/channel-state/outbound`.
- `n8n/test/workflows/whatsapp-handoff.json` sanitizes the operator summary before bot registration and Meta forward delivery.
- `n8n/test/workflows/whatsapp-template-messages.json` centralizes customer-visible `es-CO` replies and uses only named placeholder credentials.
- `n8n/test/docs/contract.md` documents the fixed bot contract, credential placeholders, assertion claims, and the active/previous `kid` rotation flow.
- `OpenRestoApi.Tests/Infrastructure/N8nPhase4WorkflowContractTests.cs` statically enforces the Phase 4 security invariants against the exported JSON and contract document.

### Phase 5 criteria confirmed
- `AdminRestaurantWhatsAppSettingsController` now exposes SuperAdmin-only `GET` and `PUT` admin settings operations guarded by both `SuperAdminOnly` and current-credential validation, with `es-CO` not-found and validation messages.
- `RestaurantWhatsAppSettingsService` now serves persisted settings and rejects enabling the test channel without a configured handoff number while normalizing the stored WhatsApp destination.
- `AdminOccasionCatalogController` is now aligned to the same current SuperAdmin management boundary as the newer credential-management surfaces.
- `openresto-frontend/api/admin.ts` now includes typed admin functions for WhatsApp settings and per-restaurant occasion catalog CRUD.
- `openresto-frontend/app/admin/settings.tsx` now renders a SuperAdmin-only `WHATSAPP DE PRUEBA` section with the planned `WhatsAppTestSettingsCard`, `HandoffWhatsAppCard`, and `OccasionCatalogCard`.
- The new Expo components use `es-CO` copy, local validation, and race-safe loading guards so operators cannot save stale default state before the current restaurant settings are loaded.
- Focused backend allow/deny tests, frontend API/component tests, scoped typecheck, scoped lint, and scoped format checks all passed in the current worktree.

### Residual risks
- Phase 6 topology, edge exposure, and secret-injection work remain intentionally unimplemented.
- The exported workflows were validated by import and static contract inspection, but not by live Meta/WABA execution because real secrets, webhook registration, and public test domains remain `USER/AWAITING` and Phase 6-scoped.
- Existing package-vulnerability restore warnings for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` remain in the baseline and were not changed by this phase.
- Existing analyzer warnings in the baseline solution remain outside the Phase 5 scope.
- `gitleaks` flags two pre-existing redacted test fixtures in frontend tests as generic-api-key false positives.
- Full `openresto-frontend` `npm run check` still reports pre-existing Prettier drift in unrelated files under `components/layout/` and `i18n/`; the changed Phase 5 files themselves pass scoped Prettier and oxlint checks.

### Final verification statement
- Test-only Phases 1 through 5 complete on Wednesday, July 29, 2026.
- Phase 5 now adds the planned admin/backend Expo settings slice while keeping public topology, DNS, and secret-injection work deferred to Phase 6.
- No production promotion performed.
