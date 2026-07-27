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
