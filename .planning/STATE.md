# State

## Planning Status
- Onboarding completed for the brownfield repo.
- A verified codebase map exists at `.planning/codebase/CODEBASE_MAP.md`.
- The binding design is captured at `.planning/features/internal-operator-mcp/SPEC.md`.
- The implementation plan and recorded local verification are under `.planning/features/internal-operator-mcp/`.
- A new Level-C planning set for the test-only WhatsApp reservation channel now exists under `.planning/features/whatsapp-reservations-test/`.
- The current feature branch already contains partial OpenResto-side WhatsApp channel implementation work that has been assessed against `develop` and folded into the new planning baseline.
- Phases 3 through 7 for `whatsapp-reservations-test` have now been executed in-worktree with a durable `n8n-test` foundation, explicit state storage, versioned workflow exports, contract documentation, SuperAdmin-only admin settings APIs, Expo WhatsApp settings cards, a webhook-only test edge topology, and a Meta/WABA operator runbook that marks all external work `USER/AWAITING`. No test deployment, DNS mutation, or external webhook activation has occurred.

## Implemented Decisions
- The remote Streamable HTTP MCP server lives in the existing ASP.NET Core backend at `/api/mcp/operator`.
- Durable `OperatorPrincipal` records use an `OperatorRestaurantScope` join table.
- Opaque short-lived operator credentials are stored only as digests and resolve to an operator principal.
- Escalation writes an audit record and uses the existing internal notification architecture.

## Local Implementation Evidence
- Implementation commit: `3bf56e6 feat(api): add authenticated operator MCP server`.
- Full backend suite was run locally in the .NET 10 SDK container; consult `VERIFICATION.md` for the recorded command and results.
- No push, merge, or deployment was performed from this worktree.
- The WhatsApp test stack now includes a checked-in `n8n-test` compose foundation with Postgres-backed state, persistent volumes for session, dedupe, ordering, replay, and outbound correlation artifacts, and a bridged bot boundary that prevents direct `n8n` reachability to the OpenResto backend.
- Phase 4 now adds importable workflow exports for Meta verification, inbound routing, confirmation state, handoff, observability, and `es-CO` template replies, plus static tests that enforce the fixed bot contract and workflow security invariants.

## Next Recommended Step
- For `internal-operator-mcp`, obtain independent review approval, then decide whether to deploy the feature branch to the isolated `test-rest` environment.
- For `whatsapp-reservations-test`, Phase 8 remains locally verified but incomplete. As of the 02:09 UTC readiness gate on July 31, the focused boundary/security suite passed (12/12), Compose rendered in a clean `env -i` environment with synthetic non-secret values and failed closed without its immutable n8n digest, and targeted n8n/runbook Gitleaks scans found no leaks. The active Docker host has no `test-rest` stack. Deployment is blocked because the externally injected contract is partial: backend/base values, n8n encryption, immutable n8n digest, webhook base URL, bot/internal credentials, and active WhatsApp assertion configuration are absent. Phase 8 cannot close until those test-only inputs, Meta/WABA/test-number provisioning, test DNS/TLS (`n8n-test` does not resolve), webhook registration/template prerequisites, and a healthy test-rest edge (currently `502`) are available. No production action is included.
