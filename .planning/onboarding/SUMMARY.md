# Onboarding Summary

## Project State
- PROJECT.md: present
- REQUIREMENTS.md: present
- ROADMAP.md: present
- STATE.md: present

## Codebase Context
- Brownfield repo: yes
- Map readiness: complete for onboarding and feature planning
- Codebase map: `.planning/codebase/CODEBASE_MAP.md`
- Fast map available: no separate fast map created
- Current database posture: SQLite remains the deployed default; PostgreSQL provider wiring, migrations assembly, overlay, and backup scripts exist in-repo but require migration-repair planning before any cutover.

## Docs Context
- Existing ADR/PRD/SPEC/RFC candidates: 2 plan documents under `docs/plans/`
- Existing migration runbook evidence: `docs/backup-restore.md` plus `scripts/postgres-backup.sh`, `scripts/postgres-restore.sh`, and `scripts/postgres-restore-drill.sh`

## Recommended Next Step
- `gsd-discuss-phase postgres-migration-backups`
