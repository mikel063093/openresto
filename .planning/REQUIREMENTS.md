# Requirements

## Source Of Truth For This Planning Pass
- User-confirmed constraints from the August 17, 2026 onboarding request.
- Verified repository evidence from the inspected backend/provider wiring, PostgreSQL overlay, scripts, docs, and GitHub Actions workflows.
- Existing migration planning under `.planning/features/postgres-migration-backups/`.

## Confirmed Scope
- Planning target: repair and complete the SQLite-to-PostgreSQL migration path for OpenResto.
- Allowed work in this pass: create or update GSD planning artifacts only.
- Disallowed work in this pass: application-code edits, destructive commands, commits, pushes, or deployment actions.
- Migration posture: test-first and rollback-first. No production cutover planning may assume success without isolated test cutover evidence.

## Functional Requirements
- The system must retain SQLite as the currently recoverable source of truth until PostgreSQL cutover acceptance criteria pass in test.
- The repository must define a one-shot SQLite-to-PostgreSQL data-conversion tool with explicit source/destination validation, clean-destination enforcement, deterministic import order, sequence reseeding, and post-import invariants.
- The conversion path must preserve primary keys, foreign keys, UTC timestamps, enum values, nullable fields, and all persisted operational data required by OpenResto, including admin/auth, notifications, branding, social links, operator/channel data, and migration history expectations.
- The PostgreSQL runtime topology must remain internal-only with no public database port exposure.
- The PostgreSQL operational model must define least-privilege roles for:
  - initial bootstrap/migration application
  - steady-state application runtime
  - logical backup execution
  - restore-drill validation
- Backup and restore operations must remain logical dump based (`pg_dump` / `pg_restore`) rather than raw data-directory copying.

## Verification And Acceptance Requirements
- SCRAM-compatible PostgreSQL bootstrap and authentication must be verified in the release overlay and in backup/restore drill procedures.
- Backup verification must include custom-format dump creation, `pg_restore --list --verbose`, checksum generation, and a disposable restore drill that proves the archive materializes a non-empty public schema.
- CI must add PostgreSQL integration coverage before deployment approval. SQLite-only migration safety checks are insufficient for provider-cutover sign-off.
- A test-only cutover must have explicit acceptance criteria covering:
  - immutable SQLite snapshot preserved before conversion
  - conversion report and row-count/invariant checks recorded
  - PostgreSQL-backed application startup and health checks passing
  - admin login and representative booking flows passing
  - restore-drill evidence recorded against the PostgreSQL backup artifacts
- Rollback acceptance criteria must prove:
  - the retained SQLite snapshot remains untouched and startable
  - rollback steps are documented against the exact test release candidate
  - rollback can be executed before PostgreSQL becomes the only writable source

## Non-Functional Requirements
- Recommendations must stay compatible with ASP.NET Core 10, EF Core 10, Docker Compose, Nginx, and the repository’s existing test/deploy patterns.
- Do not rely on unverified managed services, hosted databases, or secret distribution systems not evidenced in the repo.
- Preserve current public booking and admin behavior unless a later implementation phase explicitly changes them.
- Prefer additive, phase-gated migration work over all-at-once conversion.
