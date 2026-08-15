# Phase 1: Baseline And Contracts - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Convert the approved localization roadmap into an executable implementation contract by reconciling the existing inventories, current frontend/backend localization seams, preserved-content boundaries, and the test surfaces that will enforce later phases. This phase produces documentation and execution scaffolding only; it does not broaden product scope beyond the approved `en` and `es-CO` effort.

</domain>

<decisions>
## Implementation Decisions

### Scope and locale contract
- **D-01:** The approved locale scope is product-owned copy in `en` and `es-CO` only; no other locale work is introduced in this milestone.
- **D-02:** Frontend locale selection must evolve from the current `Locale = "en" | "es"` implementation to an explicit `es-CO` contract while keeping English fallback and explicit user override persistence.
- **D-03:** Locale-aware formatting is part of the contract surface, not an optional cleanup. Existing `undefined` locale formatting calls are migration targets.

### Preservation and contract safety
- **D-04:** Restaurant-authored content remains verbatim and out of translation scope, including names, descriptions, highlights, footer copy, social labels, and other tenant-managed text pulled from the API/database.
- **D-05:** Backend localization may change only user-visible message strings. HTTP status codes, response body keys, and the established `{ message }` contract stay stable.
- **D-06:** `Accept-Language` propagation must be centralized at the shared frontend API client and resolved centrally on the backend rather than through route-specific header logic.

### Quality gates
- **D-07:** The existing inventory files under `i18n/inventory/` remain the canonical backlog seed and must be reconciled against live code before and after implementation.
- **D-08:** Phase closure for implementation phases requires targeted tests during execution and full relevant frontend/backend suites at the end, plus narrow regression checks for new untranslated copy.

### the agent's Discretion
- The exact catalog key structure, backend localization implementation detail, and regression-check implementation can be chosen during later phases as long as they preserve the contracts above and fit the existing repo patterns.

</decisions>

<specifics>
## Specific Ideas

- Use the current inventories plus `docs/plans/2026-07-22-bilingual-ui.md` as the baseline audit source, but ground all execution in the live codebase rather than the earlier plan alone.
- Keep implementation incremental and phase-scoped: public UI, admin UI, API/messages, emails, then regression gates/final verification.
- Record the concrete insertion points already found in audit:
  - `openresto-frontend/context/I18nContext.tsx`
  - `openresto-frontend/i18n/locale.ts`
  - `openresto-frontend/i18n/messages.ts`
  - `openresto-frontend/api/client.ts`
  - `OpenRestoApi/Program.cs`
  - `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap and requirements
- `.planning/PROJECT.md` — approved business context, hard constraints, and key decisions for the localization effort.
- `.planning/REQUIREMENTS.md` — requirement IDs and phase traceability for locale, public/admin UI, API, mail, and quality gates.
- `.planning/ROADMAP.md` — approved six-phase execution sequence and plan inventory.
- `.planning/STATE.md` — current project state and sequential GSD expectation.

### Brownfield audit inputs
- `docs/plans/2026-07-22-bilingual-ui.md` — prior bilingual UI implementation plan and earlier audit baseline.
- `i18n/inventory/public-ui.json` — public UI untranslated/review-needed inventory.
- `i18n/inventory/admin-ui.json` — admin UI untranslated/review-needed inventory.
- `i18n/inventory/backend-api.json` — backend API/email untranslated/review-needed inventory.

### Existing implementation seams
- `openresto-frontend/context/I18nContext.tsx` — current locale provider and persisted override seam.
- `openresto-frontend/i18n/locale.ts` — locale normalization and browser detection seam.
- `openresto-frontend/i18n/messages.ts` — current frontend catalog structure and translation helper.
- `openresto-frontend/api/client.ts` — shared frontend HTTP client where locale propagation must be centralized.
- `openresto-frontend/utils/formatters.ts` and `openresto-frontend/utils/date.ts` — active-locale formatting migration points.
- `OpenRestoApi/Program.cs` — backend middleware/bootstrap seam where request language resolution will be added.
- `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs` — global backend message shaping seam.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `openresto-frontend/context/I18nContext.tsx`: already persists explicit locale choice and exposes `t()`, but only for `en|es`.
- `openresto-frontend/i18n/messages.ts`: already has typed message keys and simple plural interpolation; it can expand into the broader public/admin catalog.
- `openresto-frontend/api/client.ts`: all frontend API wrappers flow through this file, making it the right place for centralized `Accept-Language`.
- `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`: preserves the `{ message }` response contract already relied on by tests and callers.

### Established Patterns
- Frontend tests live under `openresto-frontend/tests/**` and already include i18n/formatter coverage that should be extended, not replaced.
- Backend tests are organized by controllers/services/integration areas and frequently assert `body.message`, so localization must preserve response shape.
- Product copy is scattered across route files and shared components; formatting also appears inline via `toLocaleDateString(undefined, ...)` / `toLocaleTimeString(undefined, ...)`.

### Integration Points
- Shared frontend locale provider and formatters feed both public and admin experiences.
- Shared frontend API client feeds public booking, admin auth, holds, notifications, and other API wrappers.
- Backend request pipeline and exception handling influence all API message localization and email/notification language resolution.

</code_context>

<deferred>
## Deferred Ideas

- Additional locales beyond `en` and `es-CO`.
- Translator workflow or external translation management tooling.
- SEO/route-level locale segmentation beyond the existing product scope.

</deferred>

---

*Phase: 01-baseline-and-contracts*
*Context gathered: 2026-07-23*
