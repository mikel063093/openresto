#!/usr/bin/env bash
# Verify a PostgreSQL custom archive and restore it only after explicit
# destructive confirmation. The backend must be stopped to prevent writes.
#
# Usage:
#   scripts/postgres-restore.sh --archive /secure/path/backup.dump --apply --confirm-database <POSTGRES_DB>
#   scripts/postgres-restore.sh --archive /secure/path/backup.dump --list
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILES=("-f" "$REPO_DIR/docker-compose.release.yml" "-f" "$REPO_DIR/docker-compose.postgres.yml")
CUSTOM_COMPOSE_FILES=0
ARCHIVE=""
APPLY=0
LIST_ONLY=0
CONFIRM_DATABASE=""

usage() { grep '^#' "$0" | cut -c3-; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --archive) ARCHIVE="${2:?missing archive path}"; shift 2 ;;
    --compose-file)
      if [[ "$CUSTOM_COMPOSE_FILES" -eq 0 ]]; then
        COMPOSE_FILES=()
        CUSTOM_COMPOSE_FILES=1
      fi
      COMPOSE_FILES+=("-f" "${2:?missing compose file}")
      shift 2
      ;;
    --apply) APPLY=1; shift ;;
    --list) LIST_ONLY=1; shift ;;
    --confirm-database) CONFIRM_DATABASE="${2:?missing database name}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) printf 'Unknown argument: %s\n' "$1" >&2; exit 2 ;;
  esac
done

[[ -n "$ARCHIVE" && -f "$ARCHIVE" ]] || { echo '--archive must name an existing .dump file' >&2; exit 2; }
[[ "$ARCHIVE" == *.dump ]] || { echo 'archive must use the .dump custom-archive suffix' >&2; exit 2; }
CHECKSUM="$ARCHIVE.sha256"
[[ -f "$CHECKSUM" ]] || { echo "missing checksum: $CHECKSUM" >&2; exit 2; }
command -v docker >/dev/null || { echo 'docker is required' >&2; exit 127; }
command -v sha256sum >/dev/null || { echo 'sha256sum is required' >&2; exit 127; }

# The checksum lists a basename so it stays valid after an off-host copy.
(
  cd "$(dirname "$ARCHIVE")"
  sha256sum --check "$(basename "$CHECKSUM")"
)

docker run --rm -i postgres:16-alpine pg_restore --list --verbose < "$ARCHIVE"
[[ "$LIST_ONLY" -eq 1 ]] && exit 0

[[ "$APPLY" -eq 1 ]] || { echo 'Refusing to restore without --apply (use --list for a non-destructive check).' >&2; exit 2; }
: "${POSTGRES_DB:?Export POSTGRES_DB before a destructive restore}"
[[ "$CONFIRM_DATABASE" == "$POSTGRES_DB" ]] || { echo '--confirm-database must exactly equal POSTGRES_DB' >&2; exit 2; }

BACKEND_ID="$(docker compose "${COMPOSE_FILES[@]}" ps -q backend 2>/dev/null || true)"
if [[ -n "$BACKEND_ID" ]] && [[ "$(docker inspect -f '{{.State.Running}}' "$BACKEND_ID")" == 'true' ]]; then
  echo 'Refusing restore while backend is running. Stop it first to prevent writes.' >&2
  exit 2
fi

POSTGRES_ID="$(docker compose "${COMPOSE_FILES[@]}" ps -q postgres)"
[[ -n "$POSTGRES_ID" ]] || { echo 'postgres service is not running' >&2; exit 2; }
REMOTE_ARCHIVE="/tmp/$(basename "$ARCHIVE")"
trap 'docker exec "$POSTGRES_ID" rm -f "$REMOTE_ARCHIVE"' EXIT
docker cp "$ARCHIVE" "$POSTGRES_ID:$REMOTE_ARCHIVE"

# --clean is intentionally gated above. --if-exists avoids harmless warnings
# for objects absent from an older archive; --no-owner avoids role coupling.
docker compose "${COMPOSE_FILES[@]}" exec -T postgres sh -ceu \
  'export PGPASSWORD="$POSTGRES_PASSWORD";
   exec pg_restore -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner --no-privileges "$1"' \
  sh "$REMOTE_ARCHIVE"

echo "Restore completed into database: $POSTGRES_DB"
