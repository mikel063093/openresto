---
status: complete
created: 2026-07-23
slug: api-reference-test-environment
type: quick-task
---

# Expose a stable browsable API reference in the test environment

Scope: add a browsable API reference at a stable URL for the `test-rest` environment while keeping ASP.NET Core 10 built-in OpenAPI as the single source of truth, avoiding Swashbuckle, documenting JWT-protected endpoints without publishing credentials, and protecting the docs whenever the app is not running in Development.

Planning-only constraints:
- Create planning artifacts only. No production code edits, package changes, state-table updates, commits, pushes, or deploys in this task.
- Preserve the existing OpenAPI spec path contract used elsewhere in the repo unless there is a strong reason to version or rename it.
- Keep the implementation compatible with the current ASP.NET Core 10 + `Microsoft.AspNetCore.OpenApi` stack.

## Repo Evidence

- [`OpenRestoApi/Program.cs`](/tmp/openresto-i18n-audit/OpenRestoApi/Program.cs:43) maps `app.MapOpenApi()` only in `Development` or `Testing`.
- [`docker-compose.test-rest.yml`](/tmp/openresto-i18n-audit/docker-compose.test-rest.yml:18) runs the backend with `ASPNETCORE_ENVIRONMENT=Production`, so `/openapi/v1.json` is currently unavailable there.
- [`OpenRestoApi/Extensions/ServiceCollectionExtensions.cs`](/tmp/openresto-i18n-audit/OpenRestoApi/Extensions/ServiceCollectionExtensions.cs:180) already registers `services.AddOpenApi();`, which should remain the single OpenAPI source of truth.
- [`OpenRestoApi/OpenRestoApi.csproj`](/tmp/openresto-i18n-audit/OpenRestoApi/OpenRestoApi.csproj:20) already includes `Microsoft.AspNetCore.OpenApi` and does not include Swashbuckle.
- [`nginx/default.conf.template`](/tmp/openresto-i18n-audit/nginx/default.conf.template:68) already proxies `/openapi/` to the backend, but there is no browsable UI route and no auth gate around docs routes.
- The API uses JWT bearer auth and many controllers/actions are protected with `[Authorize]` or policy-based authorization, so the reference needs explicit auth metadata instead of implying anonymous access.

## External Compatibility Notes

- Microsoft’s current ASP.NET Core 10 docs state that `Microsoft.AspNetCore.OpenApi` generates documents only and that interactive UIs must be added separately. They also show OpenAPI customization through document and operation transformers.
- Scalar’s current ASP.NET Core integration docs show direct compatibility with `Microsoft.AspNetCore.OpenApi` via `builder.Services.AddOpenApi();`, `app.MapOpenApi();`, and `app.MapScalarApiReference();`, with customizable routes such as `/docs` or `/api-docs`.

Source links:
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0
- https://github.com/scalar/scalar/blob/main/documentation/integrations/aspnetcore/integration.md
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0

## Recommended Design

Stable URLs:
- UI route: `/api-reference`
- Spec route: keep `/openapi/v1.json`

Primary implementation choices:
1. Keep `Microsoft.AspNetCore.OpenApi` as the only OpenAPI generator and continue serving the runtime-generated document from the backend.
2. Add a UI-only integration using `Scalar.AspNetCore` because it is explicitly documented to work with `Microsoft.AspNetCore.OpenApi` and does not require Swashbuckle.
3. Introduce an explicit config gate for non-development exposure, for example `OpenApi:ExposeDocs` or equivalent env var, defaulting to `false`.
4. In `Development`, keep docs open as they are now.
5. In non-development environments, only map `/openapi/v1.json` and `/api-reference` when the explicit docs-exposure flag is enabled.
6. Protect both docs routes in non-development at the reverse proxy boundary, not by publishing app credentials in the UI. Use test-environment secrets for docs access, then let Scalar’s bearer auth input accept a user-supplied admin JWT for protected endpoint calls.
7. Add OpenAPI transformers so the generated document declares the JWT bearer security scheme and marks non-`[AllowAnonymous]` operations as requiring bearer auth.

Why this route split:
- `/api-reference` is human-friendly and stable for operators.
- Keeping `/openapi/v1.json` avoids breaking the existing spec path already referenced by repo docs and ZAP-related guidance.
- Reverse-proxy protection works for both the JSON document and the UI without weakening API authorization itself.

## Proposed Execution Plan

1. Extract docs exposure behind an explicit policy.
   - Refactor the current `Development || Testing` gate in [`Program.cs`](/tmp/openresto-i18n-audit/OpenRestoApi/Program.cs:43) into a small helper or clearly named condition.
   - Preserve open docs in `Development`.
   - Permit docs in non-development only when a dedicated config flag is set.

2. Add the compatible UI.
   - Add `Scalar.AspNetCore`.
   - Map Scalar at `/api-reference`.
   - Keep the UI route and the spec route served by the backend so both reflect the same runtime-generated OpenAPI document.

3. Describe authentication correctly in the OpenAPI document.
   - Add a document transformer that registers the `Bearer` security scheme.
   - Add an operation transformer that applies bearer requirements to secured endpoints and skips endpoints marked with `AllowAnonymous`.
   - Do not embed example credentials, tokens, or default auth headers in the document or UI config.

4. Protect docs in non-development.
   - Extend [`nginx/default.conf.template`](/tmp/openresto-i18n-audit/nginx/default.conf.template:68) so `/openapi/` and `/api-reference` are guarded when the deployment is not Development and the docs flag is enabled.
   - Use environment-supplied secrets for that guard in `test-rest`; do not hardcode credentials in compose or source.
   - Keep the backend’s normal JWT authorization untouched for API endpoints themselves.

5. Enable the test environment intentionally.
   - Update [`docker-compose.test-rest.yml`](/tmp/openresto-i18n-audit/docker-compose.test-rest.yml:18) to opt in to docs exposure via explicit environment variables.
   - Supply separate docs-protection secrets through deployment env, not committed values.
   - Keep production-like default behavior as protected/disabled unless explicitly enabled.

6. Update operator/developer docs.
   - Document the stable URL, the need for docs-protection env vars, and the workflow for obtaining an admin JWT through the existing `/api/auth/login` flow before trying protected endpoints in Scalar.
   - Clarify that the UI documents authenticated endpoints but does not publish usable credentials.

## Tests To Add

Backend/unit or startup tests:
- Verify docs remain available in `Development` without the non-development flag.
- Verify docs are not mapped in `Production` when the explicit docs-exposure flag is absent or `false`.
- Verify docs are mapped in `Production` when the explicit docs-exposure flag is `true`.
- Verify the generated OpenAPI document includes a `Bearer` security scheme.
- Verify secured endpoints in the OpenAPI document carry a security requirement while known anonymous endpoints do not.

Integration tests:
- Add or extend `WebApplicationFactory<Program>` coverage to request `/openapi/v1.json` under the relevant environment/config combinations.
- Add coverage for the UI route if it is served directly by the backend.
- Parse the returned JSON and assert that representative endpoints such as `/api/auth/login` are anonymous while protected admin routes require bearer auth in the document.

Infra/config verification:
- Add a focused config-level test or scripted assertion that the nginx template guards both `/openapi/` and `/api-reference` when docs protection is enabled.

## Deployment Verification

Target date for verification: after implementation, against the `test-rest` stack on or after 2026-07-23.

Required checks:
1. Bring up `docker compose -f docker-compose.test-rest.yml up -d --build` with docs exposure enabled and docs-protection secrets supplied through env.
2. Confirm unauthenticated access to `/api-reference` and `/openapi/v1.json` is denied at the reverse proxy.
3. Confirm authenticated docs access succeeds and the UI loads at `/api-reference`.
4. Confirm the UI resolves the live spec from `/openapi/v1.json`.
5. Obtain an admin JWT using the existing login flow and verify a protected endpoint can be tried from the UI only after the token is supplied manually.
6. Confirm no credentials, sample tokens, or default authorization headers are rendered in the page source, config, or generated OpenAPI document.

Suggested command evidence to record during implementation:
- `curl -i https://<test-host>/api-reference`
- `curl -i https://<test-host>/openapi/v1.json`
- `curl -u "$DOCS_USER:$DOCS_PASS" -i https://<test-host>/openapi/v1.json`
- JSON assertion on `components.securitySchemes.Bearer`

## Security Risks

Risk: accidentally exposing a full endpoint inventory in non-development without an auth gate.
Mitigation: require an explicit non-development docs flag plus reverse-proxy protection; default to disabled.

Risk: documenting secured endpoints as anonymous, which would mislead operators and weaken testing discipline.
Mitigation: add bearer security metadata through OpenAPI transformers and test both anonymous and protected examples.

Risk: publishing reusable credentials or tokens in source, compose files, or the docs UI configuration.
Mitigation: use environment-supplied secrets only for docs access; require users to obtain their own JWT through the normal login flow.

Risk: relying on docs protection that is weaker than the transport boundary.
Mitigation: require TLS on the test host and avoid plain HTTP basic credentials outside trusted internal paths.

Risk: compatibility drift between .NET 10 OpenAPI output and the chosen UI.
Mitigation: pin the UI package version intentionally and keep a smoke test that loads `/api-reference` against the live `/openapi/v1.json`.

## Acceptance Criteria

- A stable, browsable API reference URL exists in the test environment.
- The API reference is driven by `Microsoft.AspNetCore.OpenApi` and not by Swashbuckle.
- Authenticated endpoints are visibly documented as bearer-protected without shipping credentials.
- Docs are protected whenever the app is not running in Development.
- Tests cover mapping/exposure rules and auth metadata in the generated document.
- Deployment verification steps are explicit enough for a follow-up implementation task to execute without reopening discovery.
