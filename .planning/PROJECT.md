# Project

## Name
OpenResto

## Summary
OpenResto is a self-hosted restaurant booking platform with an ASP.NET Core 10 + SQLite backend, an Expo-based frontend, and Docker/Nginx deployment paths for local and VPS hosting.

## Current Product Shape
- Public booking flow: availability, table holds, reservation create, booking lookup, cancel by reference/email.
- Internal admin flow: login, role-based reservation operations, restaurant settings, notifications, branding, and user management.
- Deploy model: single stack on Docker Compose with Nginx reverse proxy and SQLite-backed persistence.

## Brownfield Notes
- This is a brownfield repo with established reservation, auth, migration, and deploy patterns.
- There is no existing `.planning/` tree in the repo before this onboarding pass.
- Prior implementation plans exist under `docs/plans/` and should be treated as context, not as proof of runtime behavior.

## Planning Focus
Define a decision-ready Level-C feature named `internal-operator-mcp` for internal operators only, compatible with the existing stack and deployment model, without modifying production source during onboarding.
