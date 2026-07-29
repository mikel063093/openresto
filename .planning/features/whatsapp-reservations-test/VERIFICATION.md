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
- `n8n-test` and `reservation-bot-test` routes are exposed only as intended
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
  - `n8n-test.joypaw.tech`
  - `reservation-bot-test.joypaw.tech`
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

### Commands run on Wednesday, July 29, 2026
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~WhatsAppChannelReservationsIntegrationTests|FullyQualifiedName~WhatsAppAuthorityBaselineMigrationTests|FullyQualifiedName~OpenApiDocumentationTests"`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet sln openresto.sln add OpenRestoReservationBot/OpenRestoReservationBot.csproj OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoReservationBot.Tests/OpenRestoReservationBot.Tests.csproj`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln`
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/repo zricethezav/gitleaks:latest dir /repo --no-banner --redact`

### Command summaries
- Focused backend suite: passed, `18` passed, `0` failed, `0` skipped, duration `7 s`.
- Full backend suite: passed, `1271` passed, `0` failed, `0` skipped, duration `1 m 17 s`.
- Focused bot suite: passed, `19` passed, `0` failed, `0` skipped, duration `1 s`.
- Full solution suite: passed. `OpenRestoReservationBot.Tests`: `19` passed, `0` failed, `0` skipped. `OpenRestoApi.Tests`: `1281` passed, `0` failed, `0` skipped, duration `1 m 23 s` for the backend project inside solution execution.
- Static secret scan: `gitleaks` completed and reported `2` findings, both existing redacted frontend test fixtures:
  - `openresto-frontend/tests/components/admin/settings/OperatorCredentialsCard.test.tsx`
  - `openresto-frontend/tests/api/admin.test.ts`
  - No new Phase 2 secret exposure was identified.

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

### Residual risks
- Phase 3+ n8n stack, workflows, admin frontend, and test-topology work remain intentionally unimplemented.
- Existing package-vulnerability restore warnings for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` remain in the baseline and were not changed by this phase.
- Existing analyzer warnings in the baseline solution remain outside the Phase 2 scope.
- `gitleaks` flags two pre-existing redacted test fixtures in frontend tests as generic-api-key false positives.

### Final verification statement
- Test-only Phases 1 and 2 complete on Wednesday, July 29, 2026.
- No production promotion performed.
