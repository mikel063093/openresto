# Phase 6: Regression Gates And Final Verification - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning and execution

<domain>
## Phase Boundary

Phase 6 closes the localization baseline by adding durable regression detection and proving the final bilingual contract end-to-end. It must catch new product-owned raw UI copy and backend user-facing message regressions without blocking legitimate tenant-authored content, then record exact passing evidence from the full relevant frontend/backend/runtime verification matrix.

</domain>

<decisions>
## Implementation Decisions

- **D-01:** Regression checks must target stable, reviewable product-copy sinks rather than broad blanket scanning that rejects tenant-authored content or technical literals.
- **D-02:** Backend regression coverage must ensure controller `{ message }` bodies remain wired to the central `ApiLocalization` seam, with only explicit dynamic-pattern exceptions.
- **D-03:** Final verification must include both automated coverage and runtime proof on the isolated source-built stack already running at `http://localhost:5062`.
- **D-04:** Phase 6 may fix branch-attributable localization defects surfaced by the new gates, but it must not widen scope into unrelated refactors or deployment work.
- **D-05:** `.planning/STATE.md`, `.planning/ROADMAP.md`, and verification artifacts must only record commands and results that actually passed in this worktree.

</decisions>

<specifics>
## Specific Ideas

- Frontend regression seams:
  - `openresto-frontend/app/+not-found.tsx`
  - `openresto-frontend/app/(user)/_layout.tsx`
  - `openresto-frontend/app/(user)/locations/index.tsx`
  - `openresto-frontend/components/booking/HoldStatusBanner.tsx`
  - `openresto-frontend/components/restaurant/LocationListItem.tsx`
- Backend regression seams:
  - `OpenRestoApi/Infrastructure/Localization/ApiLocalization.cs`
  - `OpenRestoApi/Controllers/BrandController.cs`
  - `OpenRestoApi/Controllers/EmailSettingsController.cs`
  - `OpenRestoApi/Controllers/MediaController.cs`
- Regression tooling:
  - `scripts/check-i18n-regressions.cjs`
  - `i18n/regression-allowlists.json`
- Final verification surfaces:
  - full frontend Jest
  - frontend format/lint/type/build checks available in the repo
  - full backend test suite via the .NET SDK container
  - meaningful public/admin Playwright smoke against `localhost:5062`

</specifics>

<canonical_refs>
## Canonical References

- `.planning/PROJECT.md`
- `.planning/REQUIREMENTS.md`
- `.planning/ROADMAP.md`
- `.planning/STATE.md`
- `.planning/phases/02-public-ui-localization/02-VERIFICATION.md`
- `.planning/phases/03-admin-ui-localization/03-VERIFICATION.md`
- `.planning/phases/04-api-locale-propagation-and-messages/04-VERIFICATION.md`
- `.planning/phases/05-emails-and-notification-copy/05-VERIFICATION.md`
- `docs/plans/2026-07-22-bilingual-ui.md`

</canonical_refs>

<deferred>
## Deferred Ideas

- Expanding localization support beyond `en` and `es-CO`
- Translator workflow or external TMS integration
- Deployment or release automation changes

</deferred>

---

*Phase: 06-regression-gates-and-final-verification*
*Context gathered: 2026-07-23*
