# Roadmap

## Active Planning Track

### Level C: `internal-operator-mcp`
Status: planned

Goal:
Design and then implement a remote HTTPS MCP surface for internal operators that can manage only the reservations they created, using short-lived scoped credentials and server-enforced restaurant/ownership authorization.

Primary artifacts:
- `.planning/features/internal-operator-mcp/SPEC.md`
- future implementation plans and execution artifacts under the same feature directory

Expected execution phases:
1. Identity and credential foundation
2. Booking ownership data model and server authorization
3. Internal API surface for operator-scoped reservation access
4. Remote MCP transport, tool contracts, and observability
5. Verification and rollout hardening

### Level C: `whatsapp-reservations-test`
Status: planned

Goal:
Complete the isolated WhatsApp Business reservation channel in test only, with `n8n-test` as Meta verifier and sole assertion issuer, `reservation-bot-test` as constrained orchestration service, and OpenResto as the only reservation authority.

Primary artifacts:
- `.planning/features/whatsapp-reservations-test/CONTEXT.md`
- `.planning/features/whatsapp-reservations-test/SPEC.md`
- `.planning/features/whatsapp-reservations-test/PLAN.md`
- `.planning/features/whatsapp-reservations-test/VERIFICATION.md`

Expected execution phases:
1. Reconcile OpenResto WhatsApp authority baseline
2. Lock reservation-bot internal service contract
3. Add durable n8n test stack and state storage
4. Implement n8n Meta, LLM, confirmation, and handoff workflows
5. Add admin backend and Expo UI for WhatsApp settings
6. Define test-only deployment topology and secrets injection
7. Write Meta/WABA provisioning runbook
8. Run full verification, E2E sandbox, and security review
