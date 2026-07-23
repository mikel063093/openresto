---
status: complete
created: 2026-07-23
completed: 2026-07-23
slug: api-reference-test-environment
type: quick-task
---

# API reference in test-rest — Summary

## Delivered

- Preserved ASP.NET Core 10 `Microsoft.AspNetCore.OpenApi` as the single runtime-generated API contract.
- Added Scalar `2.16.16` as the browsable UI at `/api-reference`.
- Kept the machine-readable contract at `/openapi/v1.json`; controllers and DTO schemas remain the source of truth, so routes and models update when code changes.
- Added a JWT bearer security scheme to the OpenAPI document without embedding credentials or tokens.
- Added explicit opt-in only for `test-rest` through `OpenApi__ExposeDocs=true`; the default remains disabled in deployed non-development environments.
- Enforced `SuperAdminOnly` on both documentation endpoints in deployed non-development environments. Development and isolated `Testing` hosts remain open for local tooling and CI discovery.
- Added Nginx routing for `/api-reference`, preventing the Expo fallback from intercepting the UI.
- Added documentation integration tests and operator-facing README guidance.

## Verification

- Focused OpenAPI integration tests: 2/2 passed.
- Full backend suite: 1,221/1,221 passed.
- Nginx image build and `nginx -t`: passed.
- Docker Compose configuration validation: passed.

## Security decisions

- No Swagger/Swashbuckle package was added.
- No documentation credentials, default JWTs, or secrets are present in source or Compose.
- Test-rest must be accessed with an existing SuperAdmin JWT or its authenticated HttpOnly cookie; Scalar can then accept a manually supplied bearer token for endpoint calls.
- Production remains disabled until an operator explicitly sets `OpenApi__ExposeDocs=true`.

## Follow-up

- Deploy to `test-rest` and verify anonymous requests receive 401 while a valid SuperAdmin JWT can retrieve both `/api-reference` and `/openapi/v1.json`.
