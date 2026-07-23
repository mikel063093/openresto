---
status: complete
created: 2026-07-23
slug: resolve-the-openresto-super-admin-user-c
type: quick-task
---

# Resolve super-admin user creation role serialization/RBAC defect

Scope: fix the live `POST /api/admin/users` failure when the frontend sends string role names (`SuperAdmin`, `BookingViewer`, `BookingEditor`) while preserving existing valid contracts, RBAC boundaries, and the closed localization roadmap.

Constraints:
- Work only in this worktree and current branch `feat/openresto-complete-localization`.
- Do not reopen or replace closed localization phases.
- Do not touch `/root/openresto-rbac`, push, merge, deploy, or use destructive Git operations.

Execution plan:
1. Reproduce with a failing integration test that posts string JSON role values to `POST /api/admin/users`.
2. Audit request flow, JSON binding, authorization policies, service invariants, EF schema/migrations, existing user semantics, and frontend error handling.
3. Apply the minimal backend/frontend fix so valid string roles bind cleanly and invalid role names return a clear validation response without secondary `request` noise.
4. Add focused service, integration, migration/schema, and frontend regression coverage.
5. Run exact focused and broader verification commands, record evidence, and commit locally with a conventional commit.
