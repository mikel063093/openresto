# Roadmap

## Active Planning Track

### Level C: `postgres-migration-backups`
Status: planned

Goal:
Repair and complete the SQLite-to-PostgreSQL provider migration path so OpenResto can cut over in test safely, validate rollback, and only then consider production adoption.

Primary artifacts:
- `.planning/features/postgres-migration-backups/CONTEXT.md`
- `.planning/features/postgres-migration-backups/PLAN.md`
- `.planning/features/postgres-migration-backups/VERIFICATION.md`

Expected execution phases:
1. Reconcile provider-neutral startup against the dedicated PostgreSQL migrations assembly
2. Specify and verify the explicit SQLite-to-PostgreSQL conversion tool
3. Lock least-privilege PostgreSQL roles, internal-only topology, and SCRAM-compatible operations
4. Add CI PostgreSQL integration and provider-aware migration verification
5. Define and prove test-only cutover, rollback, backup, and restore-drill acceptance gates

### Level C: `internal-operator-mcp`
Status: implemented/planned follow-up

Goal:
Maintain the existing internal operator MCP planning and execution artifacts as a separate workstream; it is not the focus of this onboarding repair pass.

Primary artifacts:
- `.planning/features/internal-operator-mcp/SPEC.md`
- implementation artifacts under the same feature directory

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
