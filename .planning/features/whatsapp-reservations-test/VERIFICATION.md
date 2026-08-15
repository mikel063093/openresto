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

### Phase 6 status
- Phase 6 completed locally on Wednesday, July 29, 2026.
- Scope executed: test-only webhook edge definition, private bot/backend topology boundary, and placeholder-only secret-injection documentation.
- No deployment, DNS mutation, push, secret creation, external Meta configuration, or production change was performed.

### Phase 7 status
- Phase 7 completed locally on Wednesday, July 29, 2026.
- Scope executed: `docs/runbooks/whatsapp-test-provisioning.md` plus the n8n README reference.
- The runbook covers Business Manager/WABA/test-number preparation, test DNS/TLS, secret placement by boundary, Meta webhook registration, templates, `kid` rotation, asymmetric migration note, and sandbox activation gate. Every external action is explicitly `USER/AWAITING`.
- No deployment, DNS mutation, push, secret creation, external Meta configuration, or production change was performed.

### Phase 8 status
- Started locally on Wednesday, July 29, 2026; not complete.
- Local regression and secret-scan gates were run. A stale Phase 4 contract assertion was corrected so that it now enforces the actual Phase 6 webhook-only edge boundary instead of the superseded no-edge statement.
- Test-environment sandbox, external Meta HMAC/webhook verification, live WABA messaging, DNS/TLS validation, and deployment smoke remain blocked on the external prerequisites listed below. No deployment was attempted.
- Revalidated on Wednesday, July 29, 2026 after the fail-closed webhook and workflow-bootstrap fixes: the full backend/bot solution suite, targeted frontend suite/typecheck/lint, and test compose rendering pass with deliberately invalid local-only values. This remains local verification, not a test deployment.

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
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln`
- `docker compose -f docker-compose.test-rest.yml config --quiet` with only deliberately invalid local validation values injected through the environment, including an explicit `N8N_TEST_IMAGE_TAG=latest`; this validates interpolation only and does not approve that mutable tag for deployment.
- `cd /tmp/openresto-whatsapp-delivery/openresto-frontend && npm test -- --runInBand tests/components/admin/settings/WhatsAppTestSettingsCard.test.tsx tests/components/admin/settings/HandoffWhatsAppCard.test.tsx tests/components/admin/settings/OccasionCatalogCard.test.tsx tests/app/admin/settings.test.tsx tests/api/admin.test.ts && npx tsc --noEmit -p tsconfig.json && npx oxlint components/admin/settings/WhatsAppTestSettingsCard.tsx components/admin/settings/HandoffWhatsAppCard.tsx components/admin/settings/OccasionCatalogCard.tsx components/admin/settings/OccasionCatalogRow.tsx app/admin/settings.tsx api/admin.ts tests/app/admin/settings.test.tsx tests/api/admin.test.ts`
- `git ls-files -z | xargs -0 grep -nEI '(META_(APP_SECRET|ACCESS_TOKEN|VERIFY_TOKEN)|OPENAI_API_KEY|WHATSAPP_ASSERTION_(ACTIVE|PREVIOUS)_KEY)=[^[:space:]]+' | grep -vE '(=|USER/AWAITING|pendiente-usuario|placeholder|example|<)' || true`

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
- Phase 6 focused topology and Nginx denial suite: passed, `6` passed, `0` failed, `0` skipped, duration `25 ms`, executed in `mcr.microsoft.com/dotnet/sdk:10.0`. Restore emitted the existing `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` high-severity dependency advisories.
- Phase 6 compose validation: passed with temporary non-secret `.invalid` values injected only for the command; `docker compose -f docker-compose.test-rest.yml config --quiet` exited `0`.
- Phase 6 n8n secret scan: passed; `gitleaks dir /repo/n8n --no-banner --redact` reported `no leaks found`.
- Phase 7 runbook review: passed. The runbook has `USER/AWAITING` markers for every external action, forbids public bot exposure and production changes, and is linked from `n8n/test/README.md`.
- Phase 8 full solution regression after correcting the stale contract assertion: passed. `OpenRestoApi.Tests`: `1298` passed, `0` failed, `0` skipped, duration `1 m 16 s`; `OpenRestoReservationBot.Tests`: `19` passed, `0` failed, `0` skipped, duration `<1 s`. Existing dependency advisories and analyzer warnings remain.
- Phase 8 full frontend regression: passed. `npm test -- --runInBand` completed with `154` suites and `1932` tests passed. Existing React `act(...)` and intentionally exercised API-error console warnings were emitted but did not fail the suite.
- Phase 8 repository secret scan: completed with `2` pre-existing generic-api-key false positives in the already-recorded frontend fixtures; the Phase 6/7 n8n and runbook targeted scans reported no leaks.
- Phase 8 n8n import rerun was not executed because the execution environment required separate approval to pull the non-standard `docker.n8n.io` image. The previous recorded import of all six workflows remains the latest import evidence.
- Phase 8 revalidation after `fdfa648`: full solution regression passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped in `1 m 18 s`; `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped in `1 s`. Restore again reported the existing high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Phase 8 revalidation frontend gate passed: `5` targeted suites and `138` tests passed, then `tsc --noEmit` and scoped `oxlint` completed with `0` errors.
- Phase 8 revalidation compose interpolation passed with local non-secret sentinel values. It did not start containers, use real credentials, or authorize the mutable `latest` n8n tag for deployment.
- Phase 8 tracked-files secret-pattern check returned no non-placeholder assignments for the checked Meta, OpenAI, or WhatsApp assertion variable names.
- Scheduled Phase 8 revalidation: the initial `dotnet test openresto.sln --no-restore` failed because the disposable SDK container did not have the host-mounted NuGet analyzer packages. Re-running with restore remediated the environment issue and passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (1 m 20 s); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (670 ms). Existing `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` high-severity advisory warnings and baseline analyzer warnings remain.
- Scheduled Phase 8 compose validation passed with non-secret sentinel values and `N8N_TEST_IMAGE_TAG=1.123.0`; no services were started. `git diff --check develop...HEAD` passed. The only test-only edge configuration changed since `develop` is `traefik/test-rest.yml`; no production compose, production Traefik, or `nginx-vps` files changed.
- Scheduled Phase 8 secret scan: `gitleaks` again reported exactly two pre-existing `generic-api-key` findings in `openresto-frontend/tests/components/admin/settings/OperatorCredentialsCard.test.tsx:45` and `openresto-frontend/tests/api/admin.test.ts:106`. The tracked sensitive-assignment audit found no non-placeholder Meta, OpenAI, assertion-key, or internal-credential assignment.
- Scheduled external discovery: `test-rest.joypaw.tech` resolves but its unauthenticated `/api/health` HTTPS probe returns `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` also does not resolve, as required for the private bot. No test or production infrastructure mutation was attempted.

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
- Phase 8 bootstrap remediation (current worktree): `n8n-test-workflow-init` imports the six versioned exports into the durable Postgres store and uses the n8n CLI to publish only the Meta GET and inbound POST workflows before `n8n-test` may start. The compose contract now requires an explicitly audited `N8N_TEST_IMAGE_TAG`; it no longer defaults to `latest`. A clean local Postgres import on the previously recorded `latest` image proved the CLI behavior: import completed with all six inactive, and `n8n update:workflow --id=<id> --active=true` then made exactly the two webhook workflows active. The production bootstrap shell loop is static-tested but awaits a pinned test image and external test deployment for end-to-end confirmation.
- The Phase 6 topology is repository-defined and locally validated only. It has not been deployed to the test environment and no DNS/router or external webhook has been activated.
- The exported workflows were validated by import and static contract inspection, but not by live Meta/WABA execution because real secrets, webhook registration, public test DNS/TLS, Meta/WABA/test-number provisioning, and applicable template approval remain `USER/AWAITING`.
- Phase 8 cannot close without: a Meta app/WABA and registered test phone number, externally injected test secrets, approved test DNS/TLS and test-stack deployment, Meta webhook registration (and templates if the messaging window requires them), then a test-only sandbox authorization.
- Live test-environment discovery on this run: `n8n-test.joypaw.tech` does not resolve and `reservation-bot-test.joypaw.tech` does not resolve (the latter is expected because the bot is intentionally private). `test-rest.joypaw.tech` resolves but returned `502` to an unauthenticated HTTPS probe. These observations confirm there is no usable deployed WhatsApp test edge; no infrastructure change was attempted.
- Existing package-vulnerability restore warnings for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` remain in the baseline and were not changed by this phase.
- Existing analyzer warnings in the baseline solution remain outside the Phase 5 scope.
- `gitleaks` flags two pre-existing redacted test fixtures in frontend tests as generic-api-key false positives.
- Full `openresto-frontend` `npm run check` still reports pre-existing Prettier drift in unrelated files under `components/layout/` and `i18n/`; the changed Phase 5 files themselves pass scoped Prettier and oxlint checks.

### Final verification statement
- Test-only Phases 1 through 7 are complete locally on Wednesday, July 29, 2026.
- Phase 6 provides only a test-stack definition: Traefik exposes `n8n-test` webhook paths while the bot, n8n editor/API, and OpenResto private WhatsApp API remain non-public. External activation remains `USER/AWAITING`.
- No production promotion performed.

### Scheduled Phase 8 revalidation — Wednesday, July 29, 2026 23:30 UTC
- `docker run --rm -v /tmp/openresto-whatsapp-delivery:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln` passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (`1 m 24 s`); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (`966 ms`). Restore repeated the existing high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with only local, non-secret validation sentinels and `N8N_TEST_IMAGE_TAG=1.123.0`; no containers were started.
- Targeted `gitleaks` scans of `n8n/` and `docs/runbooks/` both passed with `no leaks found`.
- External discovery remains blocked: `n8n-test.joypaw.tech` does not resolve; `test-rest.joypaw.tech/api/health` and the public private-route probe both returned `502`. A `404` boundary result cannot be established until the existing test-rest edge is healthy. `reservation-bot-test` remains intentionally non-public.
- No deployment, DNS/router mutation, secret creation, push, or production action was attempted.

### Scheduled Phase 8 security remediation — Thursday, July 30, 2026
- Closed the test-stack P1 topology finding in `docker-compose.test-rest.yml`: `backend` is now attached only to `test-rest-app-internal`; `n8n-test` retains its distinct `test-rest-bot-internal` and `test-rest-egress` paths, so it cannot resolve or connect directly to `test-rest-backend`. `reservation-bot-test` remains the only bridge between the two internal networks.
- Replaced mutable `N8N_TEST_IMAGE_TAG` interpolation with mandatory `N8N_TEST_IMAGE_DIGEST` image references (`docker.n8n.io/n8nio/n8n@sha256:…`) for both the workflow init and runtime services. Compose now fails closed when that digest is absent; the runbook and test configuration examples were updated without adding secrets.
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter 'FullyQualifiedName~TestRestN8nStackTopologyTests'` passed: `4` passed, `0` failed. Restore retained baseline `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` vulnerability advisories and pre-existing analyzer warnings.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed using only local non-secret sentinels and a synthetic `sha256:` digest; repeating the command with `N8N_TEST_IMAGE_DIGEST` absent failed with the expected required-variable error. No services were started.
- `git diff --check` passed; a changed-file secret-pattern review found no high-confidence credential material. `gitleaks` was unavailable on this runner (`command not found`), so no gitleaks result is claimed for this run.
- External discovery remains blocked: `test-rest.joypaw.tech/api/health` and the public private-route probe returned `502`; `n8n-test.joypaw.tech` does not resolve; the bot hostname does not resolve as intended. No deployment, DNS/router mutation, secret creation, push, or production action was attempted.
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln` passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (`1 m 21 s`); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (`823 ms`). The same known package-vulnerability advisories were emitted; no new test failure occurred.

### Scheduled Phase 8 deployment-readiness check — Thursday, July 30, 2026 01:43 UTC
- Confirmed the current Docker context is the remote host `srv1422242`; no `test-rest` compose project, test-stack containers, or `test-rest-*` Docker networks are deployed there. The pre-existing OpenResto stack was inspected only and was not modified.
- The externally injected test variables required for deployment are all absent from this job environment, including the Meta credentials, OpenAI credential, OpenResto/bot credentials, assertion keys, n8n database/encryption/basic-auth settings, and the mandatory `N8N_TEST_IMAGE_DIGEST`. No values were printed or created.
- `docker compose -f docker-compose.test-rest.yml config --quiet` correctly failed closed while `N8N_TEST_IMAGE_DIGEST` was absent, with the expected required-variable error. This prevented an unsafe partial deployment.
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter 'FullyQualifiedName~TestRestN8nStackTopologyTests|FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~N8nPhase4WorkflowContractTests'` passed: `12` passed, `0` failed, `0` skipped. Restore repeated the known `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` high-severity advisories.
- The tracked sensitive-assignment audit and `git diff --check develop...HEAD` both passed. The production compose files and `nginx-vps` remain unchanged relative to `develop`; `traefik/test-rest.yml` is the only changed edge configuration.
- External discovery is still blocked: `test-rest.joypaw.tech/api/health` and the public private-route probe each returned `502`; `n8n-test.joypaw.tech` has no DNS record. `reservation-bot-test.joypaw.tech` also has no DNS record, which is expected because the bot must remain private. No DNS, Traefik, Docker, secret, Meta, push, or production mutation was attempted.

### Scheduled Phase 8 regression and deployment-readiness check — Thursday, July 30, 2026 02:50 UTC
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln` passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (`1 m 19 s`); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (`1 s`). Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`; no new test failure occurred.
- `npm test -- --runInBand` in `openresto-frontend` passed: `154` suites and `1932` tests passed (`74.019 s`). Existing React `act(...)` and intentionally exercised API-error console warnings were emitted but did not fail the suite.
- The focused Phase 8 topology/security suite passed: `12` passed, `0` failed, `0` skipped. It covers the n8n topology, private Nginx route denial, and Phase 4 workflow contract invariants.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with only local non-secret sentinel values, including a synthetic immutable digest. Re-running in a subshell with `N8N_TEST_IMAGE_DIGEST` unset failed closed as required. No services were started.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each completed with `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` also passed. No production compose, production Traefik, or `nginx-vps` file is changed relative to `develop`.
- Deployment remains blocked: all externally injected test variables required by the compose stack are absent; `test-rest.joypaw.tech` resolves but both the health and public-private-route HTTPS probes return `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` is also unresolved, as required because the bot is private. No test or production deployment, DNS/router change, secret creation, Meta action, or push was attempted.

### Scheduled Phase 8 regression and deployment-readiness check — Thursday, July 30, 2026 03:52 UTC
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -v /tmp/nuget-whatsapp-phase8:/tmp/nuget -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln` passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (`1 m 18 s`); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (`1 s`). Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- The focused Phase 8 boundary suite passed: `12` passed, `0` failed, `0` skipped. It covers the n8n network topology, public Nginx private-channel denial, and the versioned Phase 4 workflow contract.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinel values and a synthetic immutable digest. The same command with `N8N_TEST_IMAGE_DIGEST` unset failed closed with the required-variable error. No services were started.
- `git diff --check develop...HEAD` passed. The only changed edge file relative to `develop` is the test-only `traefik/test-rest.yml`; no production compose, production Traefik, or `nginx-vps` path is changed.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` passed with `no leaks found`; the tracked sensitive-assignment audit found no non-placeholder credential assignment. An initial multi-path Gitleaks invocation scanned outside the intended directory and reproduced two known frontend-fixture findings; it was discarded and the correctly scoped scans above are the recorded result.
- Deployment discovery: Docker context is `default` and no `test-rest` containers or networks are present. The required externally injected test configuration is absent, including the mandatory immutable n8n image digest. `test-rest.joypaw.tech` resolves but `/api/health` and the public private-channel probe both return `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` does not resolve, which is expected because the bot must remain private.
- No deployment, DNS/router mutation, secret creation, push, external Meta action, or production action was attempted.

### Scheduled Phase 8 regression and deployment-readiness check — Thursday, July 30, 2026 04:59 UTC
- `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -v /tmp/nuget-whatsapp-phase8:/tmp/nuget -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test openresto.sln` passed: `OpenRestoApi.Tests` `1299` passed, `0` failed, `0` skipped (`1 m 20 s`); `OpenRestoReservationBot.Tests` `19` passed, `0` failed, `0` skipped (`649 ms`). Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- `npm test -- --runInBand` in `openresto-frontend` passed: `154` suites and `1932` tests passed (`73.909 s`). Existing React `act(...)` and deliberately exercised API-error console warnings were emitted but did not fail the suite.
- The focused Phase 8 topology/security suite passed: `12` passed, `0` failed, `0` skipped. It covers n8n network topology, public Nginx private-channel denial, and Phase 4 workflow-contract invariants.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic immutable digest. Re-running with `N8N_TEST_IMAGE_DIGEST` unset failed closed as required. No services were started.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` both reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. No production compose, production Traefik, or `nginx-vps` path is changed relative to `develop`.
- Deployment remains blocked: no `test-rest` containers or networks are present in the active Docker context, and all required externally injected test variables are absent. `test-rest.joypaw.tech` resolves but both the health and public-private-route HTTPS probes return `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` does not resolve, as designed because the bot is private. No deployment, DNS/router mutation, secret creation, Meta action, push, or production action was attempted.

### Scheduled Phase 8 deployment-readiness check — Thursday, July 30, 2026 06:02 UTC
- Worktree was clean at `5b86987`. The active Docker context is `default`; Docker and the external `dokploy-network` are available, but no `test-rest` stack was deployed. No deployment was attempted because the required externally injected deployment contract is incomplete.
- Exact compose-variable presence audit (values never printed) found required deployment inputs absent: `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, and every active/previous WhatsApp assertion issuer, audience, `kid`, and key input. The compose rendering therefore correctly fails closed without `N8N_TEST_IMAGE_DIGEST`.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinel values and a synthetic immutable SHA-256 digest. Re-running with the digest unset failed as required. No service was started.
- Focused Phase 8 boundary/security regression passed: `docker run --rm ... dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter 'FullyQualifiedName~TestRestN8nStackTopologyTests|FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~N8nPhase4WorkflowContractTests'` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; a tracked-file audit found `0` non-placeholder sensitive assignments, and `git diff --check develop...HEAD` passed.
- External probes remain non-deployable: `test-rest.joypaw.tech/api/health` and its public private-channel probe each returned `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` is also unresolved as required for the intentionally private bot. No DNS/router, Docker, secret, Meta/WABA, push, or production mutation occurred.

### Scheduled Phase 8 fail-closed configuration remediation — Thursday, July 30, 2026 07:08 UTC
- Closed a deployment-readiness P1 in `docker-compose.test-rest.yml`: all required test deployment inputs now use Docker Compose required-variable interpolation. This includes backend base settings, the bot/internal channel credentials, n8n database/encryption/basic-auth settings, webhook base URL, Meta credentials, OpenAI credential, assertion issuer/audience/active key material, and n8n-to-bot credential. Rotation's previous key/`kid` remain optional as a pair; backend startup continues to reject a partial pair.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinel values and a synthetic immutable SHA-256 digest. Removing `N8N_TEST_META_APP_SECRET` or `CORS_ORIGINS` caused the expected fail-closed interpolation error. No services were started.
- Focused Phase 8 topology/security regression passed: `docker run --rm -e NUGET_PACKAGES=/tmp/nuget -v "$PWD:/src" -v /tmp/nuget-whatsapp-phase8:/tmp/nuget -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter 'FullyQualifiedName~TestRestN8nStackTopologyTests|FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~N8nPhase4WorkflowContractTests'` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories plus baseline analyzer warnings.
- `git diff --check` passed. A targeted compose interpolation audit found no unguarded required Meta, OpenAI, active assertion, or internal credential interpolation; tracked sensitive-assignment audit found no non-placeholder assignment.
- Deployment remains blocked: the required externally injected test variables are absent, `test-rest.joypaw.tech` public health and private-route probes return `502`, and `n8n-test.joypaw.tech` has no DNS record. `reservation-bot-test` has no public DNS record by design. No deployment, DNS/router mutation, secret creation, external Meta action, push, or production action was attempted.

### Scheduled Phase 8 test-deployment attempt — Thursday, July 30, 2026 08:12 UTC
- Re-ran the focused boundary/security regression in the .NET 10 SDK container: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` passed (`12` passed, `0` failed, `0` skipped). Restore repeated only the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories.
- Local-only synthetic-value compose rendering passed, and rendering with values cleared failed closed. Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the tracked sensitive-assignment audit passed.
- Deployment prerequisites were rechecked without printing values. The initially available job environment did not provide a usable deployment contract; in particular it lacked the mandatory immutable `N8N_TEST_IMAGE_DIGEST` before local validation values were introduced. A `docker compose up -d --build` attempt therefore reached only the image-resolution stage with the synthetic validation digest and failed with `not found`. Docker created no `test-rest` containers or networks, and no application service started.
- The test-only dynamic Traefik configuration was temporarily synchronized to the repository topology before that failed image pull. Restoring the prior test-only router requires a privileged system-file write that this scheduled runner placed behind an approval gate; no production router, DNS, or production compose path was touched. The active test edge was already unhealthy (`test-rest.joypaw.tech` health and public private-route probes return `502`), while `n8n-test.joypaw.tech` remains unresolved and the bot remains intentionally non-public.
- No test deployment succeeded; no Meta/WABA action, secret creation, push, or production action occurred. Do not retry deployment until an externally injected, audited real n8n SHA-256 image digest and the remaining test deployment variables are available, then verify the test edge before webhook registration.

### Scheduled Phase 8 test-deployment attempt — Thursday, July 30, 2026 09:17 UTC
- Re-ran the focused Phase 8 boundary/security gate in the .NET 10 SDK container. `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` passed: `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with local non-secret sentinels. Removing either the mandatory image digest or Meta app-secret input failed closed as designed. Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. No production compose, production Traefik, or `nginx-vps` file differs from `develop`.
- An authorized test-only `docker compose -f docker-compose.test-rest.yml up -d --build --remove-orphans` was attempted using the externally injected environment. It failed before any service/container/network was created because the injected immutable n8n image digest could not be resolved by `docker.n8n.io`. The registry can resolve the audited version tag used for independent read-only discovery, but the deployment must continue to require an externally injected digest rather than substituting one. The exact user-owned blocker is an invalid or unavailable `N8N_TEST_IMAGE_DIGEST` value in the test secret/injection system.
- Post-attempt inspection confirmed no `test-rest` compose services, containers, or Docker networks exist. The externally injected configuration otherwise supplied the required deployment variables; values were not printed.
- External readiness remains incomplete: `test-rest.joypaw.tech` HTTPS health and public private-channel probes return `502`, and `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test` remains intentionally non-public. No DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 10:19 UTC
- Worktree was clean at `72314c2` before this evidence update. The active Docker context is `default` (`unix:///var/run/docker.sock`); its only running OpenResto containers are the unrelated pre-existing `mike-openresto` stack. No `test-rest` compose service, container, or Docker network exists.
- Required externally injected test deployment inputs remain absent, including the backend/base settings, n8n database/encryption/basic-auth values, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta credentials, OpenAI credential, bot/internal credentials, and assertion issuer/audience/active key inputs. Values were not printed.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed only with complete local non-secret sentinels and a synthetic immutable SHA-256 digest. Re-running with `N8N_TEST_IMAGE_DIGEST` unset failed as required. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore emitted only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` also does not resolve as intended because the bot is private. No deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 11:22 UTC
- Worktree was clean at `3eb221b`. The active Docker context is `default`; it contains the unrelated pre-existing `mike-openresto` stack but no `test-rest` compose service, container, or Docker network.
- The focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore emitted the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with a complete set of local non-secret sentinel values and a synthetic immutable SHA-256 digest. Separate runs with `N8N_TEST_IMAGE_DIGEST` and `N8N_TEST_META_APP_SECRET` absent each failed closed as required. No service was started.
- Required deployment-input presence was checked without printing values. The environment is incomplete, including `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, both internal credentials, and the assertion issuer/audience/active `kid`/key inputs. Therefore no test deployment was attempted.
- The deployed test-only Traefik dynamic file at `/etc/dokploy/traefik/dynamic/test-rest.yml` exactly matches `traefik/test-rest.yml` by SHA-256. It defines the repository's existing test-rest router and webhook-only n8n router; no test stack exists to serve either new n8n backend. No router or DNS mutation was made.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 12:24 UTC
- Worktree was clean at `b38b2c5` before this gate. Docker context remains `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network exists. Compose status cannot render without the intentionally required deployment inputs, which is expected fail-closed behavior.
- Required externally injected test inputs were checked by name only and are absent: backend/base settings, n8n Postgres/encryption/basic-auth values, immutable n8n image digest, webhook base URL, Meta credentials, OpenAI credential, bot/internal credentials, and assertion issuer/audience/active `kid`/key material. No deployment was attempted and no replacement values were created.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic immutable SHA-256 digest. Independent runs with `N8N_TEST_IMAGE_DIGEST` or `N8N_TEST_META_APP_SECRET` absent failed closed as required. The optional previous assertion key pair was deliberately absent during the valid-render check; Compose emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore again emitted the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` both reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- The deployed test-only Traefik dynamic file still matches `traefik/test-rest.yml` by SHA-256. External readiness is still blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe returned `502`; `n8n-test.joypaw.tech` remains unresolved. `reservation-bot-test.joypaw.tech` remains unresolved as designed because the bot is private. No DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 13:27 UTC
- The worktree was clean at `6556efa` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network exists.
- A required-variable presence audit, performed without reading or printing values, found the deployment contract incomplete. At minimum `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, `ReservationBot__InternalCredential`, `WhatsAppChannel__InternalCallerCredential`, and the backend assertion configuration are unavailable to this job. Some sensitive n8n inputs may be injected, but the partial contract is intentionally insufficient for deployment.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed only with complete local non-secret sentinels and a synthetic immutable SHA-256 digest. Independent renders with `N8N_TEST_IMAGE_DIGEST` or `CORS_ORIGINS` absent failed closed as required. The optional previous assertion-key pair was omitted and produced only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` both reported `no leaks found`. `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256; no production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 14:31 UTC
- Worktree was clean at `d38429e` before this evidence update. The active Docker context is `default`; there are no `test-rest`, `n8n-test`, or `reservation-bot-test` containers or Docker networks.
- Required test deployment inputs were checked by name only and remain incomplete. The job does have some n8n/Meta-related values injected, but it lacks the backend/base configuration, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, bot/internal credentials, and active WhatsApp assertion configuration required by the fail-closed compose contract. No substitute, secret, or deployment was created.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic immutable SHA-256 digest. Separate renders with `N8N_TEST_IMAGE_DIGEST` and `N8N_TEST_META_APP_SECRET` removed failed as required. The optional previous assertion-key pair was absent and only emitted expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`, plus baseline analyzer warnings.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`. `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. Only `docker-compose.test-rest.yml` and `traefik/test-rest.yml` are deployment/edge paths changed relative to `develop`; no production compose or `nginx-vps` path is changed.
- The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. Earlier same-run external probes still show `test-rest.joypaw.tech/api/health` and the public private-channel probe returning `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 15:33 UTC
- Worktree was clean at `fd8cb23` before this gate. Docker context is `default`; no `test-rest` compose containers or Docker networks exist. The repository differs from `develop` in only the test-only deployment paths `docker-compose.test-rest.yml` and `traefik/test-rest.yml`; `git diff --check develop...HEAD` passed.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` plus existing analyzer warnings.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed using complete local non-secret validation sentinels and a synthetic immutable digest. Independent renders without `N8N_TEST_IMAGE_DIGEST` and without `N8N_TEST_META_APP_SECRET` both failed closed as required. Validation sentinels were cleared from the runner shell immediately after the check; no container was started.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the tracked sensitive-assignment audit completed without a non-placeholder assignment. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256.
- The actual externally injected deployment contract is absent from this scheduled job after clearing local validation sentinels: all 21 required inputs are unavailable, including backend/base values, immutable n8n digest, webhook base URL, Meta/OpenAI credentials, bot/internal credentials, and active assertion issuer/audience/`kid`/key material. No replacement value was created and no deployment was attempted.
- External readiness remains blocked: `test-rest.joypaw.tech` resolves, but `/api/health` and the public private-channel probe both return `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` is unresolved as intended because the bot must remain private. No DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 16:37 UTC
- Worktree was clean at `0de97fc` before this evidence update. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network exists. `docker compose -f docker-compose.test-rest.yml ps --all` rendered no services after the expected optional previous-key warnings.
- Required test deployment inputs were checked by presence only and are all absent in this scheduled job: backend/base settings, n8n Postgres/encryption/basic-auth settings, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta/OpenAI values, bot/internal credentials, and assertion issuer/audience/active `kid`/key material. No substitute or secret was created; deployment was not attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic SHA-256 digest. Independent renders with `N8N_TEST_IMAGE_DIGEST` or `N8N_TEST_META_APP_SECRET` unset failed as required. The optional previous key pair was omitted and emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` also does not resolve, as designed because it must remain private. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 17:40 UTC
- Worktree was clean at `28dac72` before this gate. Docker has no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network; `docker compose -f docker-compose.test-rest.yml ps --all` rendered no services. The only changed edge path relative to `develop` remains test-only `traefik/test-rest.yml`; no production compose, production Traefik, or `nginx-vps` path differs.
- Required deployment inputs were checked by presence only and are absent in this scheduled job, including backend/base settings, n8n Postgres/encryption/basic-auth values, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta/OpenAI values, bot/internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key inputs. No substitute or secret was created, so no deployment was attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic SHA-256 digest. Independent renders with `N8N_TEST_IMAGE_DIGEST` or `N8N_TEST_META_APP_SECRET` unset failed closed as required. Optional previous assertion key inputs were deliberately absent and emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` does not resolve as intended because it is private. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 18:42 UTC
- Worktree was clean at `4603a67` before this gate. The active Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network is deployed. Compose status correctly could not render without the intentionally required deployment inputs.
- A required-variable presence audit, performed without reading or printing values, found all 21 required deployment inputs unavailable in this scheduled job, including backend/base settings, n8n Postgres/encryption/basic-auth values, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta/OpenAI values, bot/internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key material. Local validation sentinels were cleared after compose rendering and the absence audit was repeated. No substitute or secret was created, so no deployment was attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic SHA-256 digest. Independent renders with `N8N_TEST_IMAGE_DIGEST` and `N8N_TEST_META_APP_SECRET` unset each failed closed as required. Optional previous assertion-key inputs were deliberately absent and emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` does not resolve as designed because it is private. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 19:45 UTC
- Worktree was clean at `3d07223` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` service/container or Docker network exists.
- All 21 required externally injected test deployment inputs are absent from this scheduled job, including backend/base settings, n8n Postgres/encryption/basic-auth values, the immutable n8n digest, webhook base URL, Meta/OpenAI inputs, bot/internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key material. Values were not read or printed, no substitute was created, and no deployment was attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels, including both sides of the shared n8n-to-bot credential and a synthetic SHA-256 digest. Independent renders with each of `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_META_APP_SECRET`, `N8N_TEST_BOT_INTERNAL_CREDENTIAL`, `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY`, and `ReservationBot__InternalCredential` unset failed closed as required. Optional previous-key inputs were omitted and produced only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 20:48 UTC
- Worktree was clean at `e90b095` before this gate. Docker context remains `default`; there are no `test-rest`, `n8n-test`, or `reservation-bot-test` containers or Docker networks.
- The externally injected deployment contract is incomplete. Presence-only checks found required backend/base settings, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key configuration unavailable. Some n8n/Meta-related inputs are present, but the partial contract is intentionally insufficient; no values were read, printed, invented, or persisted and no deployment was attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed with complete local non-secret sentinels and a synthetic SHA-256 digest. Separate renders with `N8N_TEST_IMAGE_DIGEST` and `N8N_TEST_META_APP_SECRET` unset failed closed as required. Optional previous assertion-key values were omitted and produced only expected blank-default warnings. No service was started.
- Focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file still matches `traefik/test-rest.yml` by SHA-256, and only `traefik/test-rest.yml` differs from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` and the intentionally private `reservation-bot-test.joypaw.tech` do not resolve. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 21:53 UTC
- Worktree was clean at `ba26cf6` before this gate. Docker context remains `default`; compose status could not render with the externally injected environment because required deployment inputs are intentionally fail-closed. No `test-rest`, `n8n-test`, or `reservation-bot-test` container or network is deployed.
- A presence-only audit found the externally injected deployment contract remains incomplete. Some test-only n8n/Meta inputs are present, but required backend/base settings, the immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and active assertion issuer/audience/`kid`/key inputs are unavailable. Values were not read, printed, created, or persisted, and no deployment was attempted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. A corresponding render with `N8N_TEST_META_APP_SECRET` omitted failed closed as required. Optional previous-key inputs were omitted and emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Thursday, July 30, 2026 22:57 UTC
- Worktree was clean at `d23411a` before this gate. Docker context is `default`; no `test-rest` container or network is present. No deployment was attempted.
- A presence-only audit confirmed the externally injected contract is still incomplete. Required backend/base settings, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, n8n encryption key, and active WhatsApp assertion issuer/audience/`kid`/key inputs are unavailable. Some test-only secret inputs may be present, but the incomplete contract is intentionally insufficient; values were never read or printed.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret validation values and a synthetic immutable digest. A corresponding render without `N8N_TEST_META_APP_SECRET` failed closed as required. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`, plus baseline analyzer warnings.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. An independent read-only topology review found no locally remediable P0/P1 issue: only n8n webhook paths are routed publicly, the bot has no public route, n8n cannot directly reach the backend, and the Nginx private-channel denial remains in place.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 00:02 UTC
- Worktree was clean at `48bc285` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected contract is still incomplete. Backend/base configuration, n8n encryption, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and the active assertion issuer/audience/`kid`/key inputs are absent. Some test-only n8n/Meta inputs are present; their values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. A render with the normal environment empty failed closed on the required `N8N_TEST_IMAGE_DIGEST`; no service was started. The optional previous-key pair emitted only expected blank-default warnings.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`, plus existing analyzer warnings.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. Only `docker-compose.test-rest.yml` and test-only `traefik/test-rest.yml` differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 01:06 UTC
- Worktree was clean at `fe025a9` before this gate. Docker context remains `default`; it contains only unrelated pre-existing services and no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network. No deployment was attempted.
- A presence-only audit confirmed that the externally injected deployment contract remains incomplete. Some n8n/Meta inputs are present, but backend/base settings, `N8N_TEST_ENCRYPTION_KEY`, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. Values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. Rendering with the real job environment failed before interpolation could reach the digest because another required input was absent, which is the intended fail-closed behavior. Optional previous-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; `git diff --check develop...HEAD` passed. Only `docker-compose.test-rest.yml` and test-only `traefik/test-rest.yml` differ from `develop` among deployment/edge paths, and the deployed test-only Traefik file matches the repository copy by SHA-256.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` has no A record. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 02:09 UTC
- Worktree was clean at `c748036` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network is deployed. No deployment was attempted.
- A presence-only audit confirmed the externally injected deployment contract remains incomplete. `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, and the active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. Some test-only inputs are present; their values were not read, printed, created, or persisted. The absent inputs are sufficient to block a safe test deployment.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. The same isolated render without `N8N_TEST_IMAGE_DIGEST` failed closed as required. Optional previous-key inputs were omitted and emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No locally remediable P0/P1 finding was identified; only test compose and test Traefik paths differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` does not resolve as designed because it must remain private. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 03:13 UTC
- Worktree was clean at `a3e5cf6` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected deployment contract remains incomplete. Backend/base configuration, n8n encryption, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. Some test-only n8n/Meta inputs are present; their values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. A repeat with every other required synthetic input but no `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that digest. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. No locally remediable P0/P1 finding was identified; only `docker-compose.test-rest.yml` and test-only `traefik/test-rest.yml` differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 04:15 UTC
- Worktree was clean at `6acdff0` before this gate. Docker context is `default`; there is no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network. No deployment was attempted.
- A presence-only audit confirmed all required test deployment inputs are unavailable to this job, including backend/base settings, n8n Postgres/encryption/basic-auth values, `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta/OpenAI values, both bot/OpenResto internal credentials, and active assertion issuer/audience/`kid`/key configuration. Values were neither read nor printed, and no substitutes were created.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. A second isolated render with every other required synthetic value but without `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that required digest. Optional previous-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No locally remediable P0/P1 finding was identified; only `docker-compose.test-rest.yml` and test-only `traefik/test-rest.yml` differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 05:19 UTC
- Worktree was clean at `f5eb19c` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected deployment contract remains incomplete. Some n8n/Meta inputs are present, but `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, both bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. Values were neither read nor printed, and no substitutes were created.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. A second isolated render with every other required synthetic value but without `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that required digest. Optional previous-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`, plus baseline analyzer warnings.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. No locally remediable P0/P1 finding was identified; only `docker-compose.test-rest.yml` and test-only `traefik/test-rest.yml` differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 06:23 UTC
- Worktree was clean at `028be7f` before this evidence update. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network exists. No deployment was attempted.
- A presence-only audit confirmed that the externally injected test deployment contract remains incomplete. The job has a subset of n8n/Meta-related inputs, but lacks backend/base configuration, n8n encryption, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key material. Values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. A second isolated render with every other required synthetic value but without `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that required digest. Optional previous-key inputs produced only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories, plus baseline analyzer warnings.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No locally remediable P0/P1 finding was identified; only test-only `traefik/test-rest.yml` differs from `develop` among edge/production configuration paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 07:27 UTC
- Worktree was clean at `925425f` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed that all required externally injected test deployment inputs are absent from this job, including backend/base configuration, n8n Postgres/encryption/basic-auth settings, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, Meta/OpenAI inputs, bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key material. Values were not read, printed, created, or persisted.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories and existing analyzer warnings.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. A separate isolated render with every other required synthetic value but without `CORS_ORIGINS` failed closed specifically on that required variable. Optional previous assertion-key inputs emitted only expected blank-default warnings. No service was started.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the tracked sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256. No locally remediable P0/P1 finding was identified; only test-only `docker-compose.test-rest.yml` and `traefik/test-rest.yml` differ from `develop` among deployment/edge paths.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 ingress-hardening remediation — Friday, July 31, 2026 08:34 UTC
- An independent read-only topology review found a P1 resource-exhaustion gap on the public test-only n8n webhook edge: the webhook router had no request-body cap, rate limit, or in-flight request ceiling before n8n processed the raw Meta body.
- Remediated the test-only `n8n-test-webhooks-secure` Traefik router with three attached middlewares: a 1 MiB request cap (256 KiB in-memory threshold), a 20-request in-flight ceiling, and a 30 requests/minute average with a 60-request burst. No production Traefik, production compose, DNS, or deployed router was modified.
- Added topology regression assertions for all three middleware names and their thresholds, and documented the guardrails in the n8n test-stack README.
- `docker run --rm ... mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter 'FullyQualifiedName~TestRestN8nStackTopologyTests|FullyQualifiedName~NginxPrivateChannelExposureTests|FullyQualifiedName~N8nPhase4WorkflowContractTests'` passed: `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` plus baseline analyzer warnings.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. Python loaded `traefik/test-rest.yml` and asserted the precise router/middleware structure and values. Optional previous assertion-key inputs emitted only expected blank-default warnings. No service was started.
- Targeted Gitleaks scans of `traefik/test-rest.yml` and `n8n/test/` each reported `no leaks found`; `git diff --check` passed.
- Deployment input presence was checked without reading values. The contract remains incomplete: backend/base configuration, n8n encryption, immutable image digest, webhook base URL, both bot/OpenResto internal credentials, and active assertion issuer/audience/`kid`/key configuration are absent. Some separately injected n8n/Meta inputs may be present; no values were read, printed, created, or persisted.
- Docker context remains `default` with no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network. Certificate-validating probes returned `502` for `test-rest.joypaw.tech/api/health` and the public private-channel path; `n8n-test.joypaw.tech` remains unresolved. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 test-edge synchronization — Friday, July 31, 2026 09:41 UTC
- Independent deployment-state inspection found a P1 configuration-drift issue: the active test-only dynamic Traefik file at `/etc/dokploy/traefik/dynamic/test-rest.yml` still lacked the repository's 1 MiB request-body cap, 20-request in-flight limit, and 30/minute (60 burst) rate limit for the public `n8n-test` webhook router.
- Remediated only that approved test-edge drift by atomically replacing `/etc/dokploy/traefik/dynamic/test-rest.yml` with the committed `traefik/test-rest.yml`. SHA-256 of both files is now `d0bfbf7d84a9ca258121d7fa292e3ea6dd439c28c74f09acd1e4b16a06de583c`. Traefik's file provider is configured with `watch: true`; no production router, DNS, compose, secret, Meta/WABA, push, or production configuration was modified.
- Removed the unneeded, non-published local Traefik validation container (`trusting_khayyam`); no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network remains.
- Focused topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity advisories for `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3`.
- An isolated synthetic-value `docker compose -f docker-compose.test-rest.yml config --quiet` render passed; a render without `N8N_TEST_IMAGE_DIGEST` failed closed as designed. Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed.
- Deployment remains blocked: all required externally injected test deployment inputs are absent in this job, `test-rest.joypaw.tech/api/health` and the public private-channel probe return `502`, and `n8n-test.joypaw.tech` remains unresolved. `reservation-bot-test` remains intentionally non-public. No test stack deployment was attempted.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 10:43 UTC
- Worktree was clean at `05554a2` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network is deployed. No deployment was attempted.
- A presence-only audit found the externally injected test deployment contract remains incomplete. The job has a subset of n8n/Meta inputs, but lacks `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, both bot/OpenResto internal credentials, and active assertion issuer/audience/`kid`/key material. Values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed under `env -i` with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. The corresponding render without `N8N_TEST_IMAGE_DIGEST` failed closed as required. Optional previous-key inputs produced only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; `git diff --check develop...HEAD` and the tracked sensitive-assignment audit passed. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- The deployed test-only Traefik dynamic file still matches `traefik/test-rest.yml` by SHA-256 (`d0bfbf7d84a9ca258121d7fa292e3ea6dd439c28c74f09acd1e4b16a06de583c`). External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe returned `502`; `n8n-test.joypaw.tech` is unresolved. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 11:47 UTC
- Worktree was clean at `7fd2f77` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected test deployment contract remains incomplete. Required backend/base configuration, immutable `N8N_TEST_IMAGE_DIGEST`, webhook base URL, both bot/OpenResto internal credentials, and active assertion issuer/audience/`kid`/key configuration are absent. A subset of n8n/Meta inputs may be present; no value was read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. A corresponding isolated render without the image digest failed closed as required. Optional previous-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated only the known high-severity `Microsoft.OpenApi` and `SQLitePCLRaw.lib.e_sqlite3` advisories.
- Targeted Gitleaks scans of `n8n/` and `docs/runbooks/` each reported `no leaks found`; the deployment-path sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file still matches `traefik/test-rest.yml` by SHA-256. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- External readiness remains blocked: `test-rest.joypaw.tech/api/health` and the public private-channel probe each returned `502`; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 12:54 UTC
- Worktree was clean at `1cb9a3e` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected deployment contract is incomplete. `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, and the active WhatsApp assertion issuer/audience/`kid`/key configuration are absent. A subset of n8n/Meta and internal-credential inputs is present, but the incomplete contract is intentionally insufficient; values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. A corresponding render without `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that required input. Optional previous assertion-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore reported the known high-severity transitive advisories for `Microsoft.OpenApi 2.0.0` (`GHSA-v5pm-xwqc-g5wc`) and `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (`GHSA-2m69-gcr7-jv3q`). They resolve through the baseline `Microsoft.AspNetCore.OpenApi 10.0.10` and `Microsoft.EntityFrameworkCore.Sqlite 10.0.10` dependency paths; no unreviewed package upgrade was made in this deployment-gate pass.
- Targeted `gitleaks dir` scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the deployment-path sensitive-assignment audit and `git diff --check develop...HEAD` passed. Only the test-only `docker-compose.test-rest.yml` and `traefik/test-rest.yml` differ from `develop` among deployment/edge paths; no production compose, production Traefik, or `nginx-vps` path differs.
- Certificate-validating probes still returned `502` for `https://test-rest.joypaw.tech/api/health` and the public private-channel probe. `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` is intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 13:58 UTC
- Worktree was clean at `4b5c7a1` before this gate. Docker context is `default`; no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network exists. No deployment was attempted.
- A presence-only audit confirmed the externally injected test deployment contract remains incomplete. `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, and active WhatsApp assertion issuer/audience/`kid`/key configuration are absent. A subset of n8n/Meta and internal-credential inputs is present; values were not read, printed, created, or persisted.
- The focused Phase 8 boundary/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore repeated baseline analyzer warnings and the known high-severity advisories for `Microsoft.OpenApi 2.0.0` and `SQLitePCLRaw.lib.e_sqlite3 2.1.11`.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret validation values and a synthetic immutable SHA-256 digest. A corresponding isolated render without `N8N_TEST_IMAGE_DIGEST` failed closed as required. Optional previous assertion-key inputs emitted only expected blank-default warnings. No service was started.
- Targeted `gitleaks dir` scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the deployment-path sensitive-assignment audit and `git diff --check develop...HEAD` passed. No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256 (`d0bfbf7d84a9ca258121d7fa292e3ea6dd439c28c74f09acd1e4b16a06de583c`). Certificate-validating probes returned `502` for both `https://test-rest.joypaw.tech/api/health` and the public private-channel probe; `n8n-test.joypaw.tech` does not resolve. `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 15:00 UTC
- Worktree was clean at `99db0e6` before this gate. Docker context is `default`; rendering compose with the externally injected environment stops at the required `ADMIN_PASSWORD` interpolation, and no `test-rest`, `n8n-test`, or `reservation-bot-test` container or network is deployed. No deployment was attempted.
- A presence-only audit confirmed the test deployment contract remains incomplete. `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, both bot/OpenResto internal credentials, and active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. A subset of n8n/Meta inputs is injected; values were not read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. The same isolated render with the digest omitted failed closed specifically on `N8N_TEST_IMAGE_DIGEST`; optional previous assertion-key inputs produced only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore reported the known high-severity advisories for `Microsoft.OpenApi 2.0.0` and `SQLitePCLRaw.lib.e_sqlite3 2.1.11`; no package change was made.
- Targeted `gitleaks dir` scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file still matches `traefik/test-rest.yml` by SHA-256 (`d0bfbf7d84a9ca258121d7fa292e3ea6dd439c28c74f09acd1e4b16a06de583c`).
- Certificate-validating probes returned `502` for both `https://test-rest.joypaw.tech/api/health` and the public private-channel probe. `n8n-test.joypaw.tech` is unresolved; `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.

### Scheduled Phase 8 deployment-readiness gate — Friday, July 31, 2026 16:03 UTC
- Worktree was clean at `be6faec` before this gate. Docker context is `default`; it has only unrelated pre-existing services and no `test-rest`, `n8n-test`, or `reservation-bot-test` container or Docker network. No deployment was attempted.
- A presence-only audit confirmed the externally injected deployment contract remains incomplete. `ADMIN_EMAIL`, `CORS_ORIGINS`, `JWT_KEY`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_IMAGE_DIGEST`, `N8N_TEST_WEBHOOK_BASE_URL`, and active WhatsApp assertion issuer/audience/`kid`/key inputs are absent. Some n8n/Meta and internal-credential inputs are present, but the incomplete contract is intentionally insufficient; no values were read, printed, created, or persisted.
- `docker compose -f docker-compose.test-rest.yml config --quiet` passed in an `env -i` process with complete synthetic non-secret values and a synthetic immutable SHA-256 digest. The corresponding isolated render without `N8N_TEST_IMAGE_DIGEST` failed closed specifically on that required input. Optional previous assertion-key inputs emitted only expected blank-default warnings. No service was started.
- Focused Phase 8 topology/security regression passed in `mcr.microsoft.com/dotnet/sdk:10.0`: `TestRestN8nStackTopologyTests`, `NginxPrivateChannelExposureTests`, and `N8nPhase4WorkflowContractTests` reported `12` passed, `0` failed, `0` skipped. Restore reported baseline analyzer warnings and the known high-severity advisories for `Microsoft.OpenApi 2.0.0` and `SQLitePCLRaw.lib.e_sqlite3 2.1.11`; no package change was made.
- Targeted `gitleaks dir` scans of `n8n/` and `docs/runbooks/` reported `no leaks found`; the sensitive-assignment audit and `git diff --check develop...HEAD` passed. The deployed test-only Traefik dynamic file matches `traefik/test-rest.yml` by SHA-256 (`d0bfbf7d84a9ca258121d7fa292e3ea6dd439c28c74f09acd1e4b16a06de583c`). No production compose, production Traefik, or `nginx-vps` path differs from `develop`.
- Certificate-validating probes returned `502` for both `https://test-rest.joypaw.tech/api/health` and the public private-channel probe. `n8n-test.joypaw.tech` remains unresolved; `reservation-bot-test.joypaw.tech` remains intentionally non-public. No Docker deployment, DNS/router mutation, Meta/WABA action, secret creation, push, or production action occurred.
