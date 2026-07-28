# State

## Planning Status
- Onboarding completed for the brownfield repo.
- A verified codebase map exists at `.planning/codebase/CODEBASE_MAP.md`.
- The binding design is captured at `.planning/features/internal-operator-mcp/SPEC.md`.
- The implementation plan and recorded local verification are under `.planning/features/internal-operator-mcp/`.
- A new Level-C planning set for the test-only WhatsApp reservation channel now exists under `.planning/features/whatsapp-reservations-test/`.
- The current feature branch already contains partial OpenResto-side WhatsApp channel implementation work that has been assessed against `develop` and folded into the new planning baseline.

## Implemented Decisions
- The remote Streamable HTTP MCP server lives in the existing ASP.NET Core backend at `/api/mcp/operator`.
- Durable `OperatorPrincipal` records use an `OperatorRestaurantScope` join table.
- Opaque short-lived operator credentials are stored only as digests and resolve to an operator principal.
- Escalation writes an audit record and uses the existing internal notification architecture.

## Local Implementation Evidence
- Implementation commit: `3bf56e6 feat(api): add authenticated operator MCP server`.
- Full backend suite was run locally in the .NET 10 SDK container; consult `VERIFICATION.md` for the recorded command and results.
- No push, merge, or deployment was performed from this worktree.

## Next Recommended Step
- For `internal-operator-mcp`, obtain independent review approval, then decide whether to deploy the feature branch to the isolated `test-rest` environment.
- For `whatsapp-reservations-test`, execute Phase 1 from `.planning/features/whatsapp-reservations-test/PLAN.md` and keep all work test-only with no production promotion path.
