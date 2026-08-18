# Project

## Name
OpenResto

## Summary
OpenResto is a self-hosted restaurant booking platform with an ASP.NET Core 10 backend, an Expo-based frontend, and Docker/Nginx deployment paths for local, test, and VPS hosting. The current deployed default remains SQLite-backed, while this repository now also contains PostgreSQL provider wiring, a dedicated PostgreSQL migrations assembly, a PostgreSQL release overlay, and backup/restore drill scripts that require a coordinated migration-repair planning pass before any cutover.

## Current Product Shape
- Public booking flow: availability, table holds, reservation create, booking lookup, cancel by reference/email.
- Internal admin flow: login, role-based reservation operations, restaurant settings, notifications, branding, and user management.
- Deploy model: single stack on Docker Compose with Nginx reverse proxy; release base remains SQLite-first, with a PostgreSQL overlay available only for reviewed PostgreSQL-capable builds.

## Brownfield Notes
- This is a brownfield repo with an existing `.planning/` tree, prior feature plans, and mixed historical planning state that no longer fully matches the repository contents.
- The backend now selects `sqlite` or `postgres` via `DATABASE_PROVIDER`, but the repo still carries substantial SQLite-specific startup, migration, and test assumptions that make this a high-risk provider migration repair rather than a greenfield PostgreSQL rollout.
- `OpenRestoApi.PostgresMigrations/`, `docker-compose.postgres.yml`, `docs/backup-restore.md`, and `scripts/postgres-*.sh` provide partial migration infrastructure, but the inspection evidence does not yet prove an end-to-end safe SQLite-to-PostgreSQL cutover path.

## Planning Focus
Produce decision-ready planning for the SQLite-to-PostgreSQL migration repair only. The immediate planning target is the existing Level-C feature `postgres-migration-backups`, with explicit capture of:
- the one-shot SQLite-to-PostgreSQL data-conversion tool requirements
- least-privilege PostgreSQL runtime and backup roles
- SCRAM-compatible backup and restore-drill verification
- CI PostgreSQL integration before any deployment approval
- test-only cutover and rollback acceptance criteria
