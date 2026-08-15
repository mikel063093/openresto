# Concerns

**Analysis Date:** 2026-07-23

## Primary Risks

### Contract Regression
- Backend callers and tests rely on current status codes and `{ message }` response bodies.
- Localization must only change user-visible text, not payload shapes or routing semantics.

### Authored Content Contamination
- Restaurant names, descriptions, highlights, footer copy, and similar values are tenant-authored data.
- Translating these accidentally would violate explicit scope and damage restaurant intent.

### Partial i18n Drift
- The repo already contains locale detection, an i18n context, tests, and a language selector.
- Plans must reconcile existing behavior instead of assuming a blank-slate localization architecture.

### Formatting Inconsistency
- Many UI surfaces still use `toLocaleDateString(undefined, ...)`, which can diverge from the selected product locale.
- Fixing this centrally is necessary to avoid repeated regressions.

### Backend Message Scatter
- User-facing backend strings appear across controllers, exception handling, and email flows.
- Without a central catalog seam, translations will drift and reviewability will stay poor.

## Operational Constraints

- This onboarding pass is planning only; no source edits, installs, commits, pushes, or deploys are allowed.
- Future execution phases should respect the repo's large test surface and avoid oversized all-at-once migrations.

## Recommended Planning Bias

- Work in vertical brownfield slices with explicit gates at each phase.
- Land central locale and contract seams before broad copy migration where possible.
- Treat inventories as backlog evidence, not as proof that every runtime path is already covered.

---
*Concern analysis: 2026-07-23*
*Update when major risks change*
