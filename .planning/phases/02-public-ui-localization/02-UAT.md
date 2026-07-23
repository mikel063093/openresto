# Phase 02 UAT

## Objective

Verify guest-facing product copy and formatting behave correctly in `en` and `es-CO` while tenant-authored restaurant content remains unchanged.

## Checks

1. Home page default product copy renders from the catalog in both locales.
2. Lookup flow labels, not-found copy, cancellation confirmation, and recent-booking summaries render in `en` and `es-CO`.
3. Booking confirmation labels, status copy, map/calendar actions, and copy-to-clipboard affordances render in `en` and `es-CO`.
4. Shared public detail rows and date pickers use active-locale formatting rather than environment-default locale output.
5. Restaurant-authored names, addresses, and highlight text remain verbatim.
