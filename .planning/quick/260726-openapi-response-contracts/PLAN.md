---
status: completed
created: 2026-07-26
slug: openapi-response-contracts
type: quick-task
---

# Enrich the protected OpenAPI reference with response contracts and examples

## Evidence

The live protected spec (`/openapi/v1.json`) contains 87 operations. It has no JSON response schema on 84 operations and no examples on any operation. Most actions return `IActionResult`, so the generated document cannot infer response DTOs or non-200 status codes on its own.

The runtime API contract must remain unchanged. Actual handled domain errors use `MessageResponse` (`{"message":"..."}`) for 400/404/409/500, while framework model validation and unmatched API routes use RFC 7807 `ValidationProblemDetails` / `ProblemDetails`. Authorization is JWT/cookie based and all docs remain SuperAdmin-protected outside Development/Testing.

Implementation completed in this worktree on 2026-07-26:

- Added explicit MVC response metadata across the controller surface for success, 204, auth, validation, domain-error, not-found, conflict, rate-limit, and 500 cases without changing runtime handlers or payloads.
- Added narrowly scoped ASP.NET Core built-in OpenAPI transformers plus OpenAPI-only response model types so the generated document can merge duplicate 400 shapes, attach safe schema/media examples, and preserve real `MessageResponse` vs RFC 7807 contracts.
- Replaced the prior OpenAPI smoke test with spec-parsing assertions that cover representative public/admin GET, POST/201, 204, auth, `ProblemDetails`, `ValidationProblemDetails`, `MessageResponse`, and the global “no empty response map” invariant.
- Runtime behavior/security remains unchanged in code review: docs are still built with built-in ASP.NET Core OpenAPI + Scalar, the bearer scheme transformer remains, and no Swashbuckle dependency was added.
- Verification: focused OpenAPI integration test passed (2/2), and the complete backend suite passed (1,221/1,221) in `mcr.microsoft.com/dotnet/sdk:10.0`.

## Requirements

1. Document every generated controller operation with accurate success response status code(s) and response DTO schemas.
2. Document applicable 400, 401, 403, 404, 409, 429, and 500 outcomes without changing runtime responses.
3. Show safe, representative JSON examples for successful DTO responses, `MessageResponse`, `ProblemDetails`, and `ValidationProblemDetails`; never include passwords, tokens, real emails, persistent IDs, or secrets.
4. Preserve ASP.NET Core 10 built-in OpenAPI + Scalar; add no Swashbuckle dependency.
5. Keep current URL/auth/exposure behavior. Do not modify booking/business logic, database schema, API serialization, or existing HTTP responses.
6. Add integration/spec tests asserting representative public/admin operations have correct status schemas and examples, and all operations have a documented response.

## Design

- Use ASP.NET Core MVC `ProducesResponseType` / `ProducesDefaultResponseType` metadata as the source of truth for per-action success and special error contracts. Apply it to all controller actions, matching actual branches and existing status codes.
- Add only narrowly-scoped OpenAPI transformers to provide cross-cutting, accurate standard error descriptions/examples and schema-level safe examples. Do not synthesize inaccurate operation-specific DTOs from route names.
- Use reusable standard error schemas: `MessageResponse`, `ProblemDetails`, and `ValidationProblemDetails` as appropriate. Preserve the existing `{ message }` exception handler contract.
- Include XML summary/comments only when they improve public contract meaning; do not mass-add noisy implementation comments.

## Acceptance checks

- Generated spec exposes JSON response content/schema for representative GET, POST-created, PUT/PATCH, DELETE/204, public booking, hold, authentication, and admin endpoints.
- Generated spec includes a safe example for representative success, domain error (`message`), validation problem, and authorization problem responses.
- No operation has an empty response map.
- Current OpenAPI authentication requirements and protected docs behavior are unchanged.
- Focused OpenAPI integration tests and full backend suite pass; deployed test-rest spec is parsed and sampled after deployment.
