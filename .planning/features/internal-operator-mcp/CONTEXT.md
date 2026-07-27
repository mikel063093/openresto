# Internal Operator MCP Context

## Source
- Binding scope: `.planning/features/internal-operator-mcp/SPEC.md`
- Discussion mode: non-interactive, user-locked decisions supplied in the implementation request on July 27, 2026.

## Locked Decisions

### Deployment and transport
- The remote MCP transport will be implemented inside the existing ASP.NET Core backend process.
- The deployed topology remains the existing Nginx TLS terminator in front of the backend.
- The MCP surface must be real remote HTTP MCP with Streamable HTTP compatibility, not a custom JSON imitation.
- The implementation will use the official maintained C# SDK packages:
  - `ModelContextProtocol`
  - `ModelContextProtocol.AspNetCore`
- The MCP endpoint path will be mapped explicitly under the existing API namespace at `/api/mcp/operator`.
- Stateless Streamable HTTP mode is preferred for MVP because the feature only needs request/response tool execution and should not depend on session state or server-initiated prompts.

### Authentication and authorization
- MCP requests authenticate only with opaque bearer credentials in the `Authorization` header.
- No cookie fallback is allowed on MCP routes, even though admin JWT auth currently supports cookies elsewhere.
- Credential validation must enforce:
  - digest-based lookup only, never plaintext storage
  - expiry
  - revocation
  - active operator
  - restaurant scope
  - ownership-aware access checks for reservation resources
- Resource fetches for out-of-scope or foreign-owned bookings should avoid enumeration leakage. Prefer not-found semantics at the resource layer while still auditing the denial.

### Identity and persistence model
- Use a durable `OperatorPrincipal`.
- Use `OperatorRestaurantScope` as the operator-to-restaurant join table.
- Use `OperatorAgentCredential` for opaque short-lived agent credentials associated to an operator.
- Store only secure credential digests and credential metadata. Never persist plaintext bearer tokens.
- `Booking` gains nullable ownership fields so legacy/public/admin rows remain backward compatible.
- Reservation ownership for MCP flows is based on the durable operator principal, not the transient credential record.

### Escalation MVP
- Escalation must persist a durable audit/escalation record.
- Escalation must enqueue an internal notification using the existing notification queue/worker architecture when compatible.
- No external webhook integration is allowed in MVP.

## Required Codebase Fits
- Follow the established controller -> service -> repository/DbContext structure.
- Reuse existing booking, hold, availability, cancellation, and UTC handling rules.
- Do not weaken or broaden public/admin routes or their existing auth flows.
- Follow current EF Core + SQLite additive migration patterns with snapshot updates and migration tests.
- Follow existing error-shape conventions, keeping machine-readable codes additive on the new internal/operator surfaces.

## Security Boundaries
- Backend authorization is the security boundary.
- MCP auth must be isolated from admin browser auth behavior.
- Rate limiting must add a credential-aware partition on the server side, not only IP-based limits.
- Audit mutation and escalation activity with operator id, credential id, restaurant id, reservation id, action, outcome, and request correlation where available.
- Preserve UTC persistence semantics.

## Implementation Implications
- A dedicated MCP auth scheme/handler is required so MCP routes do not inherit admin cookie fallback.
- Internal operator APIs should sit behind the same auth context used by MCP tools so the MCP layer remains thin and domain logic stays testable.
- Booking create/update/cancel flows need operator-aware variants that stamp ownership and deny cross-operator mutation.
- Notification queue abstractions need an additive work item for escalation events.
- Nginx and Docker updates should be limited to the new MCP path exposure and any route-specific rate-limit settings required for the feature.

## TDD Execution Rule
- Every behavior slice starts with a focused test.
- Execute the test in RED first.
- Implement the smallest production change to make it pass.
- Re-run GREEN before moving to the next slice.
- Security deny paths are mandatory in the same slice, not deferred.

## Planned Vertical Slices
1. Operator identity, credential persistence, and digest validation.
2. MCP auth scheme and credential-aware rate limiting.
3. Booking ownership persistence and service-layer authorization.
4. Operator internal reservation API.
5. Escalation audit plus internal notification enqueue.
6. MCP tool surface on `/api/mcp/operator`.
7. Migration verification, regression, and VPS-path smoke checks.

## Deferred / Explicitly Out Of Scope
- External webhooks for escalation.
- Customer-facing MCP.
- Replacing current admin JWTs or public booking lookup flows.
- Multi-instance/sessionful MCP features that require server-to-client callbacks.
