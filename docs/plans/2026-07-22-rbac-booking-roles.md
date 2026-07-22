# Booking-role RBAC Implementation Plan

> **For Hermes:** Execute directly with strict test-driven development; do not use Claude Code or GSD.

**Goal:** Replace OpenResto's single shared administrator with multiple local users and enforce three server-side roles: `SuperAdmin`, `BookingViewer`, and `BookingEditor`.

**Architecture:** Keep the existing SQLite `AdminCredentials` table for backwards-compatible upgrades, but make it multi-row by adding a role column and a unique normalized email index. Existing deployments are migrated to `SuperAdmin`. JWTs carry the user's role; authorization policies gate reservation read and write operations at the API boundary. The frontend reads the returned session role and hides non-permitted navigation/actions, while API policy enforcement remains the security boundary.

**Tech stack:** ASP.NET Core 9 authorization policies and JWT claims, EF Core/SQLite migrations, xUnit integration tests, Expo/React Native frontend.

## Permission matrix

| Capability | SuperAdmin | BookingViewer | BookingEditor |
|---|---:|---:|---:|
| All existing admin configuration/features | yes | no | no |
| Read reservations, details and supporting location/table lookup | yes | yes | yes |
| Create/update/extend/cancel/restore reservations | yes | no | yes |
| Permanently delete reservations | yes | no | no |
| User management | yes | no | no |

`BookingEditor` deliberately cannot permanently purge records, manage locations/settings, send ad-hoc booking emails, or alter users.

### Task 1: Model and migrate role-aware users

**Files:**
- Modify: `OpenRestoApi/Core/Domain/AdminCredential.cs`
- Modify: `OpenRestoApi/Infrastructure/Persistence/AppDbContext.cs`
- Create: `OpenRestoApi/Migrations/*_AddAdminCredentialRole.cs`
- Test: `OpenRestoApi.Tests/Migrations/*Role*Tests.cs`

1. Write migration/model tests asserting the existing credential becomes `SuperAdmin` and normalized emails are unique.
2. Run the focused test and observe it fail before production changes.
3. Add `AdminRole` enum, `Role` with `SuperAdmin` default, email uniqueness, migration and snapshot.
4. Re-run focused migration tests.

### Task 2: Authenticate the matched user and mint a role claim

**Files:**
- Modify: `Core/Application/Services/AuthService.cs`
- Modify: `Core/Application/Services/JwtTokenService.cs`
- Modify: `Core/Application/Interfaces/IAdminCredentialRepository.cs`
- Modify: `Infrastructure/Persistence/Repositories/AdminCredentialRepository.cs`
- Test: `OpenRestoApi.Tests/Services/AuthServiceTests.cs`
- Test: `OpenRestoApi.Tests/Services/JwtTokenServiceTests.cs`

1. Add a failing test proving that a matching non-superadmin account logs in and receives its own role claim.
2. Make the repository expose list/delete support and update authentication to look up the submitted email first.
3. Generate JWTs from the matched user’s email and role, retaining existing cookie behavior.
4. Run focused service tests.

### Task 3: Enforce read/write policies on reservation endpoints

**Files:**
- Modify: `OpenRestoApi/Extensions/ServiceCollectionExtensions.cs`
- Modify: `OpenRestoApi/Controllers/AdminController.cs`
- Modify: any admin-only controllers currently using bare `[Authorize]`
- Test: `OpenRestoApi.Tests/Integration/*Role*Authorization*Tests.cs`

1. Write integration tests where viewer gets 200 for booking reads and 403 for mutations; editor gets 200 for supported mutations and 403 for purge; superadmin retains full access.
2. Register named `BookingsRead`, `BookingsWrite`, and `SuperAdminOnly` policies.
3. Apply policies endpoint-by-endpoint and explicitly make bare authenticated administrative controllers SuperAdmin-only.
4. Run the focused integration suite and then the complete backend suite.

### Task 4: SuperAdmin user-management API

**Files:**
- Create: `OpenRestoApi/Controllers/AdminUsersController.cs`
- Create/modify: user DTOs in `Core/Application/DTOs/`
- Test: `OpenRestoApi.Tests/Integration/AdminUsersControllerTests.cs`

1. Write failing integration tests for SuperAdmin list/create/update-role/reset-password/delete operations and for non-superadmin 403 responses.
2. Implement input validation, password hashing and the invariant that the last SuperAdmin cannot be demoted/deleted.
3. Run focused tests, then full backend suite.

### Task 5: Role-aware admin frontend

**Files:**
- Modify: `openresto-frontend/api/auth.ts`
- Modify: `openresto-frontend/app/admin/_layout.tsx`
- Modify: `openresto-frontend/components/layout/AdminSidebar.tsx`
- Modify: bookings components/screens that render mutation controls
- Create: `openresto-frontend/app/admin/users.tsx`
- Test: focused Jest tests for session role, navigation and disabled/hiding mutations.

1. Extend `/me` session data to include `role` and persist it in layout state.
2. Render only the Bookings route for viewer/editor; show full navigation and a Users route to SuperAdmin.
3. Hide booking mutation controls for viewers; preserve all existing controls for SuperAdmin/editor except destructive purge for editor.
4. Run Jest formatting/lint checks and relevant UI tests.

### Task 6: Documentation, review, commit and push

**Files:**
- Modify: `README.md` or admin documentation with roles and upgrade behavior.

1. Run backend test suite and frontend check/test commands.
2. Review the diff for missing `[Authorize]` policies and role-claim regressions.
3. Commit logical units and push to `mikel063093/openresto`.
