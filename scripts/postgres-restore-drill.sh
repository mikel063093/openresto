#!/usr/bin/env bash
# Restore a production-format archive into an isolated, disposable PostgreSQL
# 16 container. It never joins a Compose network and publishes no host port.
#
# Usage: scripts/postgres-restore-drill.sh --archive /secure/path/backup.dump
set -euo pipefail

ARCHIVE=""
usage() { grep '^#' "$0" | cut -c3-; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --archive) ARCHIVE="${2:?missing archive path}"; shift 2 ;;
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

(
  cd "$(dirname "$ARCHIVE")"
  sha256sum --check "$(basename "$CHECKSUM")"
)
docker run --rm -i postgres:16-alpine pg_restore --list --verbose < "$ARCHIVE" >/dev/null

suffix="$(date -u +%Y%m%d%H%M%S)-$$"
container="openresto-postgres-restore-drill-$suffix"
password="$(openssl rand -hex 24)"
cleanup() { docker rm -f "$container" >/dev/null 2>&1 || true; }
trap cleanup EXIT

docker run -d --rm --name "$container" \
  -e POSTGRES_DB=restore_drill \
  -e POSTGRES_USER=restore_drill \
  -e POSTGRES_PASSWORD="$password" \
  -e POSTGRES_INITDB_ARGS=--auth=scram-sha-256 \
  postgres:16-alpine >/dev/null

for _ in {1..30}; do
  if docker exec "$container" pg_isready -h 127.0.0.1 -U restore_drill -d restore_drill >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
docker exec "$container" pg_isready -h 127.0.0.1 -U restore_drill -d restore_drill >/dev/null

remote_archive="/tmp/$(basename "$ARCHIVE")"
docker cp "$ARCHIVE" "$container:$remote_archive"
docker exec "$container" sh -ceu \
  'export PGPASSWORD="$POSTGRES_PASSWORD";
   exec pg_restore -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --no-privileges "$1"' \
  sh "$remote_archive"
table_count="$(docker exec "$container" sh -ceu 'export PGPASSWORD="$POSTGRES_PASSWORD"; psql -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Atqc "SELECT count(*) FROM information_schema.tables WHERE table_schema = '\''public'\'';"')"
[[ "$table_count" =~ ^[1-9][0-9]*$ ]] || { echo 'restore drill failed: archive produced no public tables' >&2; exit 1; }

echo "Restore drill passed: PostgreSQL 16 restored $table_count public table(s) in disposable container $container"
