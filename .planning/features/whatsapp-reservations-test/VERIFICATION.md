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
