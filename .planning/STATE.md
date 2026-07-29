# State

## Planning Status
- Onboarding completed for the brownfield repo.
- A verified codebase map exists at `.planning/codebase/CODEBASE_MAP.md`.
- The binding design is captured at `.planning/features/internal-operator-mcp/SPEC.md`.
- The implementation plan and recorded local verification are under `.planning/features/internal-operator-mcp/`.
- A new Level-C planning set for the test-only WhatsApp reservation channel now exists under `.planning/features/whatsapp-reservations-test/`.
- The current feature branch already contains partial OpenResto-side WhatsApp channel implementation work that has been assessed against `develop` and folded into the new planning baseline.
- Phase 3 for `whatsapp-reservations-test` has now been executed in-worktree with a durable `n8n-test` foundation, explicit state storage, and private-only network segmentation; public topology/DNS remains deferred to Phase 6.

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

## Next Recommended Step
- For `internal-operator-mcp`, obtain independent review approval, then decide whether to deploy the feature branch to the isolated `test-rest` environment.
- For `whatsapp-reservations-test`, Phase 4 is the next recommended step: add actual Meta/LLM/confirmation/handoff workflows on top of the Phase 3 durable `n8n-test` foundation while keeping public topology, DNS, and edge routing deferred until Phase 6.
