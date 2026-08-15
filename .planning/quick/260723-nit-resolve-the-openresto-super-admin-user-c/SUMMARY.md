---
status: complete
completed: 2026-07-23
slug: resolve-the-openresto-super-admin-user-c
---

# Quick Task Summary

## Root Cause

- `POST /api/admin/users` accepted only numeric enum JSON for `AdminRole`, while the frontend correctly sent canonical string role names.
- ASP.NET Core rejected string roles during model binding before `AdminUserService` executed, producing a `400` validation `ProblemDetails` payload with noisy `errors.request` plus the default enum conversion message.
- The frontend admin-user helper only read `body.message`, so field-level validation errors from `ProblemDetails.errors.role` collapsed to the generic `"Unable to update user."` fallback.

## Fix

- Added a role-specific JSON converter for `AdminRole` that accepts canonical string names, still accepts defined numeric enum values, and serializes role fields back as canonical strings.
- Added a custom `ApiBehaviorOptions.InvalidModelStateResponseFactory` that:
  - normalizes `$.role` to `role`
  - localizes validation messages via `ApiLocalization`
  - removes redundant body-parameter `"request" field is required` noise when field-level JSON errors already exist
  - preserves the `400` validation `ProblemDetails` contract for invalid model-state requests
- Updated the frontend admin-user API helper to surface validation messages from `errors.*` when `message` is absent.

## RBAC / Schema Audit

- Authorization policies already remained correct and required no widening:
  - `SuperAdminOnly` => only `SuperAdmin`
  - `BookingsRead` => `SuperAdmin`, `BookingViewer`, `BookingEditor`
  - `BookingsWrite` => `SuperAdmin`, `BookingEditor`
- `AdminUsersController` already stayed guarded with `[Authorize(Policy = "SuperAdminOnly")]`.
- `AdminUserService` invariants remained intact:
  - only another active `SuperAdmin` permits demotion/deactivation of an active `SuperAdmin`
  - duplicate normalized emails remain rejected
- Database/migration audit result:
  - `AdminCredentials.Role` is `INTEGER NOT NULL DEFAULT 0`
  - existing legacy rows upgrade to `Role = 0` (`SuperAdmin`) without data rewriting
  - `IsActive` upgrades to `1`
  - no data migration was needed for existing role semantics

## Files Changed

- `OpenRestoApi/Core/Domain/AdminRole.cs`
- `OpenRestoApi/Infrastructure/Json/AdminRoleJsonConverter.cs`
- `OpenRestoApi/Extensions/ServiceCollectionExtensions.cs`
- `OpenRestoApi/Infrastructure/Localization/ApiLocalization.cs`
- `OpenRestoApi.Tests/Integration/AdminUsersControllerTests.cs`
- `OpenRestoApi.Tests/Services/AdminUserServiceTests.cs`
- `OpenRestoApi.Tests/Migrations/AdminCredentialRolesMigrationTests.cs`
- `openresto-frontend/api/admin.ts`
- `openresto-frontend/tests/api/admin.test.ts`
- `openresto-frontend/tests/components/admin/settings/UsersRolesCard.test.tsx`
- `openresto-frontend/tests/components/common/ModalEscapeRegression.test.tsx`

## Verification

- `docker run --rm -v /tmp/openresto-i18n-audit:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~AdminUsersControllerTests|FullyQualifiedName~AdminUserServiceTests|FullyQualifiedName~AdminCredentialRolesMigrationTests"`
  - Result: `16` tests passed.
- `docker run --rm -v /tmp/openresto-i18n-audit:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj --filter "FullyQualifiedName~RoleAuthorizationTests|FullyQualifiedName~JwtTokenServiceTests|FullyQualifiedName~AdminUsersControllerTests|FullyQualifiedName~AdminUserServiceTests|FullyQualifiedName~AdminCredentialRolesMigrationTests"`
  - Result: `26` tests passed.
- `npm test --prefix openresto-frontend -- --runInBand tests/api/admin.test.ts tests/components/admin/settings/UsersRolesCard.test.tsx`
  - Result: `2` suites passed, `127` tests passed.
- `npm test --prefix openresto-frontend -- --runInBand`
  - Result: `151` suites passed, `1931` tests passed.
- `npm run check --prefix openresto-frontend`
  - Result: passed; Prettier clean and `oxlint` completed with existing warnings only.
- `npm exec --prefix openresto-frontend tsc -- --noEmit -p openresto-frontend/tsconfig.json`
  - Result: passed.
- `npx expo export --platform web`
  - Result: passed; export written to `openresto-frontend/dist`.
- `docker run --rm -v /tmp/openresto-i18n-audit:/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:10.0 dotnet test OpenRestoApi.Tests/OpenRestoApi.Tests.csproj`
  - Result: `1219` tests passed.

## Environment Notes

- Host `dotnet` was unavailable (`dotnet: command not found`), so all backend verification ran in the .NET 10 SDK container.
