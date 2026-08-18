#!/usr/bin/env bash
# Create a consistent PostgreSQL custom-format archive, verify it, retain local
# copies, and optionally copy it to a restic repository. No credential is read
# by this script: docker compose reads deployment environment variables.
#
# Usage:
#   scripts/postgres-backup.sh [--compose-file FILE ...] [--backup-dir DIR]
#
# Optional environment-only integration:
#   RESTIC_REPOSITORY, RESTIC_PASSWORD_COMMAND (and the selected restic backend
#   variables), RESTIC_KEEP_DAILY, RESTIC_KEEP_WEEKLY, RESTIC_KEEP_MONTHLY,
#   BACKUP_ALERT_HOOK
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILES=("-f" "$REPO_DIR/docker-compose.release.yml" "-f" "$REPO_DIR/docker-compose.postgres.yml")
CUSTOM_COMPOSE_FILES=0
BACKUP_DIR="${BACKUP_DIR:-$REPO_DIR/backups/postgres}"

usage() {
  grep '^#' "$0" | cut -c3-
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --compose-file)
      if [[ "$CUSTOM_COMPOSE_FILES" -eq 0 ]]; then
        COMPOSE_FILES=()
        CUSTOM_COMPOSE_FILES=1
      fi
      COMPOSE_FILES+=("-f" "${2:?missing compose file}")
      shift 2
      ;;
    --backup-dir) BACKUP_DIR="${2:?missing backup directory}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) printf 'Unknown argument: %s\n' "$1" >&2; exit 2 ;;
  esac
done

alert_failure() {
  local status=$?
  if [[ -n "${BACKUP_ALERT_HOOK:-}" ]] && command -v curl >/dev/null 2>&1; then
    curl --fail --silent --show-error --max-time 15 \
      -X POST -H 'Content-Type: application/json' \
      --data "{\"service\":\"openresto-postgres\",\"status\":\"failed\",\"exitCode\":${status}}" \
      "$BACKUP_ALERT_HOOK" >/dev/null || true
  fi
  exit "$status"
}
trap alert_failure ERR

command -v docker >/dev/null || { echo 'docker is required' >&2; exit 127; }
command -v sha256sum >/dev/null || { echo 'sha256sum is required' >&2; exit 127; }

umask 077
mkdir -p "$BACKUP_DIR"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
ARCHIVE="$BACKUP_DIR/openresto-postgres-$TIMESTAMP.dump"
LISTING="$ARCHIVE.list"
CHECKSUM="$ARCHIVE.sha256"
TMP_ARCHIVE="$ARCHIVE.partial"
TMP_LISTING="$LISTING.partial"
trap 'rm -f "$TMP_ARCHIVE" "$TMP_LISTING"' EXIT

# pg_dump custom archives are transactional snapshots and are suitable for
# pg_restore; do not use a filesystem copy of the PostgreSQL data directory.
docker compose "${COMPOSE_FILES[@]}" exec -T postgres sh -ceu \
  ': "${POSTGRES_BACKUP_USER:?Set POSTGRES_BACKUP_USER in the deployment environment}";
   : "${POSTGRES_BACKUP_PASSWORD:?Set POSTGRES_BACKUP_PASSWORD in the deployment environment}";
   export PGPASSWORD="$POSTGRES_BACKUP_PASSWORD";
   exec pg_dump -h 127.0.0.1 -U "$POSTGRES_BACKUP_USER" -d "$POSTGRES_DB" --format=custom --no-owner --no-privileges' \
  > "$TMP_ARCHIVE"

test -s "$TMP_ARCHIVE"
docker run --rm -i postgres:16-alpine pg_restore --list --verbose < "$TMP_ARCHIVE" > "$TMP_LISTING"
test -s "$TMP_LISTING"
archive_sum="$(sha256sum "$TMP_ARCHIVE" | cut -d ' ' -f1)"
printf '%s  %s\n' "$archive_sum" "$(basename "$ARCHIVE")" > "$CHECKSUM.partial"
mv "$TMP_ARCHIVE" "$ARCHIVE"
mv "$TMP_LISTING" "$LISTING"
mv "$CHECKSUM.partial" "$CHECKSUM"

# Keep local retention separate from off-host retention. Values are days and
# intentionally configurable only through the process environment.
LOCAL_RETENTION_DAYS="${LOCAL_BACKUP_RETENTION_DAYS:-14}"
[[ "$LOCAL_RETENTION_DAYS" =~ ^[0-9]+$ ]] || { echo 'LOCAL_BACKUP_RETENTION_DAYS must be a non-negative integer' >&2; exit 2; }
find "$BACKUP_DIR" -maxdepth 1 -type f \( -name 'openresto-postgres-*.dump' -o -name 'openresto-postgres-*.dump.list' -o -name 'openresto-postgres-*.dump.sha256' \) -mtime "+$LOCAL_RETENTION_DAYS" -delete

if [[ -n "${RESTIC_REPOSITORY:-}" || -n "${RESTIC_PASSWORD_COMMAND:-}" ]]; then
  : "${RESTIC_REPOSITORY:?RESTIC_REPOSITORY is required when restic is enabled}"
  : "${RESTIC_PASSWORD_COMMAND:?RESTIC_PASSWORD_COMMAND is required when restic is enabled}"
  command -v restic >/dev/null || { echo 'restic is required when restic is enabled' >&2; exit 127; }
  restic backup --tag openresto-postgres -- "$ARCHIVE" "$LISTING" "$CHECKSUM"
  restic forget --prune --tag openresto-postgres \
    --keep-daily "${RESTIC_KEEP_DAILY:-7}" \
    --keep-weekly "${RESTIC_KEEP_WEEKLY:-4}" \
    --keep-monthly "${RESTIC_KEEP_MONTHLY:-12}"
fi

printf 'PostgreSQL backup verified: %s\n' "$ARCHIVE"
