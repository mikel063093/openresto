# Internal Operator MCP Plan

## Goal
Implement the binding spec in the existing ASP.NET Core backend with a real Streamable HTTP MCP endpoint at `/api/mcp/operator`, durable operator identity, opaque short-lived scoped credentials, ownership-safe reservation management, escalation audit/notification, and additive deployment/documentation changes.

## Constraints
- Strict TDD for each vertical slice: write focused test, run RED, implement minimal code, run GREEN.
- Preserve public and admin route behavior.
- Use controller -> service -> repository/DbContext patterns.
- Use additive SQLite migration and snapshot changes only.
- Do not store plaintext MCP credentials.
- Avoid reservation enumeration and PII leakage.

## Execution Slices

### Slice 1: Operator identity and credential foundation
- RED:
  - add unit/integration tests for credential issuance/validation, expiry, revocation, inactive operator denial, and restaurant scope resolution
  - add migration tests proving fresh-create and upgrade compatibility for new operator tables and nullable booking ownership columns
- GREEN:
  - add domain models: `OperatorPrincipal`, `OperatorRestaurantScope`, `OperatorAgentCredential`, `OperatorActionAudit`
  - extend `Booking` with nullable MCP ownership/channel fields
  - update `AppDbContext`, repositories/interfaces, and additive EF migration + snapshot
  - add digest helper/service for opaque credential generation and verification

### Slice 2: MCP auth scheme and credential-aware rate limiting
- RED:
  - auth integration tests for missing token, malformed token, expired token, revoked token, out-of-scope restaurant, and successful auth
  - rate-limit tests proving partitioning by credential instead of only IP for MCP routes
- GREEN:
  - add dedicated MCP bearer authentication handler with no cookie fallback
  - project operator/credential claims into `HttpContext.User`
  - add MCP-specific rate-limiting policy keyed by credential id/operator id
  - wire new auth/rate-limit policies in startup without changing admin auth behavior

### Slice 3: Booking ownership enforcement
- RED:
  - service tests proving MCP-created bookings stamp `CreatedByOperatorId`
  - service/integration tests for self read/update/cancel success and cross-operator denial
  - deny-path tests for out-of-scope restaurant and legacy/public/admin unaffected behavior
- GREEN:
  - add operator-aware booking service methods and repository queries
  - reuse existing availability/booking rules for create/modify/cancel
  - implement ownership-safe not-found semantics and audit writes for mutations/denials

### Slice 4: Internal operator API
- RED:
  - controller/integration tests for availability, create, list own, get own, modify own, cancel own
  - DTO validation and error-shape tests
- GREEN:
  - add internal operator controllers/DTOs/OpenAPI metadata matching local conventions
  - keep controller layer thin and delegate to operator services

### Slice 5: Escalation audit and internal notification
- RED:
  - service tests proving escalation persists an audit/escalation record
  - queue/worker tests proving escalation enqueues compatible notification work
  - integration tests for allowed self escalation and denied foreign escalation
- GREEN:
  - add escalation service/repository logic
  - extend notification queue/worker with an internal escalation work item
  - implement durable escalation audit record and compatible admin notification creation

### Slice 6: MCP transport
- RED:
  - MCP endpoint integration tests proving tool discovery and authenticated tool execution over `/api/mcp/operator`
  - deny-path tests proving unauthorized requests fail before tool execution
- GREEN:
  - add `ModelContextProtocol.AspNetCore` packages
  - register MCP server/tools in ASP.NET Core using stateless HTTP transport
  - expose operator tools backed by the internal operator services
  - document the exact endpoint path and token usage

### Slice 7: Hardening and verification
- RED/GREEN as needed:
  - migration verification for fresh DB and upgraded DB
  - regression tests for existing public/admin flows touched by the new code
  - Docker/Nginx route smoke checks for `/api/mcp/operator` behind the local HTTPS topology where feasible
  - lint/build/full backend suite

## Expected Changed Areas
- `OpenRestoApi/Core/Domain/*`
- `OpenRestoApi/Core/Application/DTOs/*`
- `OpenRestoApi/Core/Application/Interfaces/*`
- `OpenRestoApi/Core/Application/Services/*`
- `OpenRestoApi/Infrastructure/Persistence/*`
- `OpenRestoApi/Infrastructure/Notifications/*`
- `OpenRestoApi/Infrastructure/Auth/*`
- `OpenRestoApi/Controllers/*`
- `OpenRestoApi/Extensions/*`
- `OpenRestoApi/Program.cs`
- `OpenRestoApi/Migrations/*`
- `OpenRestoApi/OpenRestoApi.csproj`
- `OpenRestoApi.Tests/**/*`
- `nginx.conf`, `docker-compose.vps.yml`, and backend operational docs only if required by the actual route exposure

## Verification Gates
- Focused RED/green command transcript captured during execution.
- Full backend test suite passes.
- EF migration add/apply checks pass for fresh and upgrade paths.
- Build passes with the new MCP package and endpoint registration.
- HTTPS/Nginx smoke checks pass where the local environment supports them.
