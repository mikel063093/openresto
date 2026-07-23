---
phase: 01-baseline-and-contracts
plan: 02
subsystem: planning
tags: [i18n, contracts, preservation, testing]
requires:
  - phase: "01"
    provides: "Localization inventory map and execution seams from 01-01"
provides:
  - "Preservation and locale contract for later implementation phases"
  - "Phase 1 tracker updates in ROADMAP and STATE"
affects: [public-ui, admin-ui, api, email, regression-gates]
tech-stack:
  added: []
  patterns:
    - "Preserve tenant-authored content verbatim"
    - "Preserve HTTP status codes and JSON body keys while localizing only user-visible message strings"
key-files:
  created: [.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md]
  modified: [.planning/ROADMAP.md, .planning/STATE.md]
key-decisions:
  - "Only product-owned copy is a translation target"
  - "Later phases must run targeted tests during execution and full relevant suites at final verification"
patterns-established:
  - "Use centralized locale and transport seams rather than ad hoc per-screen/per-controller localization"
requirements-completed: [LOC-01, QUAL-01]
coverage:
  - id: D1
    description: "Recorded the locale, preservation, and HTTP/JSON contract rules that govern the implementation phases."
    requirement: "LOC-01"
    verification:
      - kind: other
        ref: ".planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md contract sections"
        status: pass
    human_judgment: false
  - id: D2
    description: "Updated roadmap and project state to mark Phase 1 complete and Phase 2 as the next active phase."
    requirement: "QUAL-01"
    verification:
      - kind: other
        ref: "rg progress/state scan over .planning/ROADMAP.md and .planning/STATE.md"
        status: pass
    human_judgment: false
duration: 14min
completed: 2026-07-23
status: complete
---

# Phase 1: Baseline And Contracts Summary

**Execution contract for `en` and `es-CO` localization with preserved tenant content, stable `{ message }` HTTP/JSON behavior, and phase-level test expectations locked into project tracking**

## Performance

- **Duration:** 14 min
- **Started:** 2026-07-23T00:18:00Z
- **Completed:** 2026-07-23T00:32:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Locked the implementation contract for later phases: product-owned copy only, tenant-authored restaurant content untouched, and only `en` plus `es-CO` in scope.
- Locked the transport contract for later phases: localized backend messages may change text only, not status codes or response body keys.
- Locked the verification contract for later phases: targeted tests during execution, then full relevant frontend/backend suites and localization-specific gates before final closure.

## Task Commits

Documentation-only phase work will be captured in the scoped Phase 1 commit after tracker updates are committed.

## Files Created/Modified
- `.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md` - Preservation, locale, and test-expectation contract summary.
- `.planning/ROADMAP.md` - Progress updated to reflect Phase 1 completion and detailed plan completion.
- `.planning/STATE.md` - Current focus advanced to Phase 2 with updated progress metrics.

## Decisions Made
- Preserve restaurant-authored values exactly as stored and rendered today. This includes restaurant names, descriptions, highlights, footer copy, social labels, and similar tenant-managed content.
- Evolve the frontend locale contract to explicit `es-CO` handling while keeping English fallback and explicit override persistence.
- Centralize `Accept-Language` at `openresto-frontend/api/client.ts` and resolve request language centrally in the backend request pipeline.
- Keep the backend response contract stable by localizing only message text, not HTTP semantics or `{ message }` payload keys.
- Treat later verification as mandatory evidence, not optional cleanup:
  - Frontend unit/component/Jest coverage for localized routes and helpers
  - Playwright guest/admin smoke coverage across both locales
  - Backend unit/integration coverage for representative `message` responses in both locales
  - Final regression checks for newly introduced untranslated copy/message gaps

## Deviations from Plan

None - plan executed as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 2 can now migrate public UI copy and formatting against a fixed preservation/contract rule set.
- Phase 4 and Phase 5 have explicit safety rails for backend/email localization before any code changes begin there.
- The next execution focus is Phase 2: public UI localization.

---
*Phase: 01-baseline-and-contracts*
*Completed: 2026-07-23*
