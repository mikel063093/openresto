#!/usr/bin/env bash
# Trusted deployment helper. The runner executes the root-owned copy installed at
# /opt/openresto-ci/bin, never the repository checkout.
set -euo pipefail
usage(){ echo 'Usage: deploy-environment.sh --environment test|production --sha <full-git-sha>'; }
ENVIRONMENT=""; EXPECTED_SHA=""
while (($#)); do case "$1" in --environment) ENVIRONMENT="${2:?}"; shift 2;; --sha) EXPECTED_SHA="${2:?}"; shift 2;; -h|--help) usage; exit 0;; *) usage >&2; exit 2;; esac; done
[[ "$ENVIRONMENT" == test || "$ENVIRONMENT" == production ]] || { echo 'invalid environment' >&2; exit 2; }
[[ "$EXPECTED_SHA" =~ ^[0-9a-f]{40}$ ]] || { echo 'sha must be a full lowercase SHA' >&2; exit 2; }
CI_ROOT="${OPENRESTO_CI_ROOT:-/opt/openresto-ci}"; export OPENRESTO_CI_ROOT="$CI_ROOT"
CONFIG="$CI_ROOT/secrets/$ENVIRONMENT.env"
[[ -f "$CONFIG" && "$(stat -c '%U:%a' "$CONFIG")" == root:600 ]] || { echo 'missing or insecure local config' >&2; exit 1; }
set -a; source "$CONFIG"; set +a
: "${DEPLOY_COMPOSE_FILES:?}" "${DEPLOY_PROJECT:?}" "${DEPLOY_PROVIDER:?}" "${DEPLOY_SERVICES:?}" "${DEPLOY_IMMUTABLE_SERVICES:?}" "${DEPLOY_URL:?}" "${DEPLOY_HEALTH_PATH:?}" "${DEPLOY_PROTECTED_PATH:?}"
[[ "$DEPLOY_PROVIDER" == sqlite || "$DEPLOY_PROVIDER" == postgres ]] || exit 2
[[ "$DEPLOY_URL" =~ ^https:// ]] || { echo 'HTTPS is required' >&2; exit 2; }
if [[ -n "${DEPLOY_VERSION_VARIABLE:-}" ]]; then [[ "$DEPLOY_VERSION_VARIABLE" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || exit 2; export "$DEPLOY_VERSION_VARIABLE=$EXPECTED_SHA"; fi
IFS=: read -r -a files <<< "$DEPLOY_COMPOSE_FILES"; compose=(docker compose --project-name "$DEPLOY_PROJECT")
for f in "${files[@]}"; do [[ -f "$f" ]] || { echo "missing compose file: $f" >&2; exit 1; }; compose+=(-f "$f"); done
if [[ -n "${DEPLOY_ENV_FILE:-}" ]]; then [[ "$(stat -c '%U:%a' "$DEPLOY_ENV_FILE")" == root:600 ]] || { echo 'insecure env file' >&2; exit 1; }; compose+=(--env-file "$DEPLOY_ENV_FILE"); fi
"${compose[@]}" config -q
# Every application artifact must be pinned to the exact promoted SHA. No source
# builds and no mutable/latest images are permitted on a privileged runner.
rendered="$("${compose[@]}" config --format json)"
RENDERED_COMPOSE="$rendered" python3 - "$EXPECTED_SHA" "$DEPLOY_IMMUTABLE_SERVICES" <<'PY'
import json,os,sys
sha,services=sys.argv[1],sys.argv[2].split()
data=json.loads(os.environ['RENDERED_COMPOSE']).get('services',{})
for service in services:
 image=data.get(service,{}).get('image','')
 if not image.endswith(':'+sha): raise SystemExit(f'non-immutable or wrong image for {service}: {image}')
 if 'latest' in image: raise SystemExit(f'mutable image for {service}')
PY
backup_dir="$CI_ROOT/backups/$ENVIRONMENT"; mkdir -p "$backup_dir"; chmod 700 "$backup_dir"
ts="$(date -u +%Y%m%dT%H%M%SZ)"; manifest="$backup_dir/deploy-$ts-$EXPECTED_SHA.manifest"
trap 'status=$?; echo "deployment failed; automatic rollback is intentionally disabled" >&2; "${compose[@]}" logs --tail=120 || true; exit "$status"' ERR
if [[ "$DEPLOY_PROVIDER" == postgres ]]; then
 : "${POSTGRES_BACKUP_USER:?}" "${POSTGRES_BACKUP_PASSWORD:?}" "${POSTGRES_RESTORE_TABLE:?}"
 archive="$backup_dir/openresto-$ts.dump"; export PGPASSWORD="$POSTGRES_BACKUP_PASSWORD" PGUSER="$POSTGRES_BACKUP_USER"
 "${compose[@]}" exec -T -e PGPASSWORD -e PGUSER postgres sh -ceu 'exec pg_dump -h 127.0.0.1 -U "$PGUSER" -d "$POSTGRES_DB" --format=custom --no-owner --no-privileges' > "$archive"
 test -s "$archive"; docker run --rm -i postgres:16-alpine pg_restore --list --verbose < "$archive" > "$archive.list"; sha256sum "$archive" > "$archive.sha256"
 drill="openresto-restore-$RANDOM"; docker run -d --name "$drill" -e POSTGRES_PASSWORD=restore-check postgres:16-alpine >/dev/null
 for _ in $(seq 1 30); do docker exec "$drill" pg_isready -U postgres >/dev/null 2>&1 && break; sleep 1; done
 docker exec "$drill" pg_isready -U postgres >/dev/null
 docker exec -i "$drill" pg_restore -U postgres -d postgres --no-owner --no-privileges < "$archive"
 docker exec "$drill" psql -U postgres -d postgres -Atqc "SELECT to_regclass('public.\"${POSTGRES_RESTORE_TABLE}\"') IS NOT NULL" | grep -qx t
 docker rm -f "$drill" >/dev/null
 printf 'provider=postgres\narchive=%s\nchecksum=%s\nrestore_drill=passed\n' "$archive" "$archive.sha256" > "$manifest"
else
 archive="$backup_dir/openresto-$ts.sqlite3"; "${compose[@]}" exec -T backend sqlite3 /data/openresto.db ".backup '/tmp/openresto-ci-backup.db'"
 id="$("${compose[@]}" ps -q backend)"; docker cp "$id:/tmp/openresto-ci-backup.db" "$archive"; docker run --rm -i keinos/sqlite3 sqlite3 /dev/stdin 'PRAGMA integrity_check;' < "$archive" | grep -qx ok; sha256sum "$archive" > "$archive.sha256"; printf 'provider=sqlite\narchive=%s\nchecksum=%s\n' "$archive" "$archive.sha256" > "$manifest"
fi
if [[ -n "${DEPLOY_MEDIA_VOLUME:-}" ]]; then
  media="$backup_dir/media-$ts.tar.gz"
  media_container="openresto-media-backup-$RANDOM"
  docker create --name "$media_container" -v "$DEPLOY_MEDIA_VOLUME:/media:ro" alpine:3.22 sh -ceu 'tar -C /media -czf /tmp/media.tar.gz .' >/dev/null
  docker start -a "$media_container" >/dev/null
  docker cp "$media_container:/tmp/media.tar.gz" "$media"
  docker rm "$media_container" >/dev/null
  sha256sum "$media" > "$media.sha256"
  printf 'media=%s\n' "$media" >> "$manifest"
fi
read -r -a services <<< "$DEPLOY_SERVICES"; "${compose[@]}" pull "${services[@]}"; "${compose[@]}" up -d --no-build --remove-orphans "${services[@]}"
for n in $(seq 1 60); do code="$(curl -fsS -o /dev/null -w '%{http_code}' "$DEPLOY_URL$DEPLOY_HEALTH_PATH" || true)"; [[ "$code" == 200 ]] && break; [[ "$n" == 60 ]] && { echo "health=$code" >&2; exit 1; }; sleep 5; done
protected="$(curl -sS -o /dev/null -w '%{http_code}' "$DEPLOY_URL$DEPLOY_PROTECTED_PATH" || true)"; [[ "$protected" == 401 || "$protected" == 403 ]] || { echo "protected=$protected" >&2; exit 1; }
printf 'sha=%s\nhealth=200\nprotected=%s\n' "$EXPECTED_SHA" "$protected" >> "$manifest"; echo "deployment succeeded: $manifest"
