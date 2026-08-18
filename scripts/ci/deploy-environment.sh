#!/usr/bin/env bash
# Deploy one configured OpenResto environment from the checked-out workflow SHA.
# Secrets/config live outside Git: $OPENRESTO_CI_ROOT/secrets/{test,production}.env.
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: deploy-environment.sh --environment test|production --sha <40-hex-sha>

The environment file must be root-owned mode 0600 and define:
  DEPLOY_COMPOSE_FILES   Colon-separated absolute/checkout-relative compose files
  DEPLOY_PROJECT         Exact Docker Compose project name
  DEPLOY_PROVIDER        sqlite | postgres
  DEPLOY_SERVICES        Space-separated core service names to start
  DEPLOY_URL             HTTPS public base URL
  DEPLOY_HEALTH_PATH     Usually /api/health
  DEPLOY_PROTECTED_PATH  Protected API/docs path expected to deny anonymously
For PostgreSQL it must additionally define POSTGRES_BACKUP_USER and
POSTGRES_BACKUP_PASSWORD through its protected environment file.
EOF
}

ENVIRONMENT=""
EXPECTED_SHA=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --environment) ENVIRONMENT="${2:?missing environment}"; shift 2 ;;
    --sha) EXPECTED_SHA="${2:?missing SHA}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done
[[ "$ENVIRONMENT" == test || "$ENVIRONMENT" == production ]] || { echo 'environment must be test or production' >&2; exit 2; }
[[ "$EXPECTED_SHA" =~ ^[0-9a-f]{40}$ ]] || { echo 'sha must be a full lowercase Git SHA' >&2; exit 2; }
[[ "$(git rev-parse HEAD)" == "$EXPECTED_SHA" ]] || { echo 'checkout SHA does not match the approved workflow SHA' >&2; exit 1; }

CI_ROOT="${OPENRESTO_CI_ROOT:-/opt/openresto-ci}"
export OPENRESTO_CI_ROOT="$CI_ROOT"
CONFIG="$CI_ROOT/secrets/$ENVIRONMENT.env"
[[ -f "$CONFIG" ]] || { echo "missing protected deployment config: $CONFIG" >&2; exit 1; }
[[ "$(stat -c '%U:%a' "$CONFIG")" == "root:600" ]] || { echo "config must be root-owned mode 0600: $CONFIG" >&2; exit 1; }
# shellcheck disable=SC1090
set -a
source "$CONFIG"
set +a
: "${DEPLOY_COMPOSE_FILES:?missing DEPLOY_COMPOSE_FILES}"
: "${DEPLOY_PROJECT:?missing DEPLOY_PROJECT}"
: "${DEPLOY_PROVIDER:?missing DEPLOY_PROVIDER}"
: "${DEPLOY_SERVICES:?missing DEPLOY_SERVICES}"
: "${DEPLOY_URL:?missing DEPLOY_URL}"
: "${DEPLOY_HEALTH_PATH:?missing DEPLOY_HEALTH_PATH}"
: "${DEPLOY_PROTECTED_PATH:?missing DEPLOY_PROTECTED_PATH}"
[[ "$DEPLOY_PROVIDER" == sqlite || "$DEPLOY_PROVIDER" == postgres ]] || { echo 'DEPLOY_PROVIDER must be sqlite or postgres' >&2; exit 2; }
[[ "$DEPLOY_URL" =~ ^https:// ]] || { echo 'DEPLOY_URL must use HTTPS' >&2; exit 2; }
if [[ -n "${DEPLOY_VERSION_VARIABLE:-}" ]]; then
  [[ "$DEPLOY_VERSION_VARIABLE" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || { echo 'invalid DEPLOY_VERSION_VARIABLE' >&2; exit 2; }
  export "$DEPLOY_VERSION_VARIABLE=$EXPECTED_SHA"
fi

IFS=':' read -r -a compose_files <<< "$DEPLOY_COMPOSE_FILES"
compose=(docker compose --project-name "$DEPLOY_PROJECT")
for compose_file in "${compose_files[@]}"; do
  [[ -f "$compose_file" ]] || { echo "compose file does not exist: $compose_file" >&2; exit 1; }
  compose+=(-f "$compose_file")
done
if [[ -n "${DEPLOY_ENV_FILE:-}" ]]; then
  [[ -f "$DEPLOY_ENV_FILE" ]] || { echo "DEPLOY_ENV_FILE does not exist" >&2; exit 1; }
  [[ "$(stat -c '%U:%a' "$DEPLOY_ENV_FILE")" == "root:600" ]] || { echo 'DEPLOY_ENV_FILE must be root-owned mode 0600' >&2; exit 1; }
  compose+=(--env-file "$DEPLOY_ENV_FILE")
fi

backup_dir="$CI_ROOT/backups/$ENVIRONMENT"
mkdir -p "$backup_dir"
chmod 700 "$backup_dir"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_manifest="$backup_dir/deploy-$timestamp-$EXPECTED_SHA.manifest"
cleanup_on_error() {
  status=$?
  echo "deployment failed; no automatic rollback is attempted because data may have changed" >&2
  "${compose[@]}" logs --tail=120 || true
  exit "$status"
}
trap cleanup_on_error ERR

"${compose[@]}" config -q
if [[ "$DEPLOY_PROVIDER" == postgres ]]; then
  : "${POSTGRES_BACKUP_USER:?missing POSTGRES_BACKUP_USER}"
  : "${POSTGRES_BACKUP_PASSWORD:?missing POSTGRES_BACKUP_PASSWORD}"
  archive="$backup_dir/openresto-$timestamp.dump"
  listing="$archive.list"
  checksum="$archive.sha256"
  export PGPASSWORD="$POSTGRES_BACKUP_PASSWORD"
  export PGUSER="$POSTGRES_BACKUP_USER"
  "${compose[@]}" exec -T -e PGPASSWORD -e PGUSER postgres sh -ceu '
    exec pg_dump -h 127.0.0.1 -U "$PGUSER" -d "$POSTGRES_DB" --format=custom --no-owner --no-privileges
  ' > "$archive"
  test -s "$archive"
  docker run --rm -i postgres:16-alpine pg_restore --list --verbose < "$archive" > "$listing"
  test -s "$listing"
  sha256sum "$archive" > "$checksum"
  printf 'provider=postgres\narchive=%s\nlisting=%s\nchecksum=%s\n' "$archive" "$listing" "$checksum" > "$backup_manifest"
else
  archive="$backup_dir/openresto-$timestamp.sqlite3"
  checksum="$archive.sha256"
  "${compose[@]}" exec -T backend sqlite3 /data/openresto.db ".backup '/tmp/openresto-ci-backup.db'"
  container_id="$("${compose[@]}" ps -q backend)"
  [[ -n "$container_id" ]] || { echo 'backend container is unavailable for SQLite backup' >&2; exit 1; }
  docker cp "$container_id:/tmp/openresto-ci-backup.db" "$archive"
  docker run --rm -i keinos/sqlite3 sqlite3 /dev/stdin 'PRAGMA integrity_check;' < "$archive" | grep -qx 'ok'
  sha256sum "$archive" > "$checksum"
  printf 'provider=sqlite\narchive=%s\nchecksum=%s\n' "$archive" "$checksum" > "$backup_manifest"
fi

# Never pull a branch here: Actions checked out the exact approved SHA. Compose must
# either build the checkout or use immutable images already pinned by the config.
read -r -a services <<< "$DEPLOY_SERVICES"
"${compose[@]}" up -d --build --remove-orphans "${services[@]}"

for attempt in $(seq 1 60); do
  code="$(curl --fail-with-body --silent --show-error --output /dev/null --write-out '%{http_code}' "$DEPLOY_URL$DEPLOY_HEALTH_PATH" || true)"
  [[ "$code" == 200 ]] && break
  [[ "$attempt" == 60 ]] && { echo "health check failed: HTTP $code" >&2; exit 1; }
  sleep 5
done
protected_code="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' "$DEPLOY_URL$DEPLOY_PROTECTED_PATH" || true)"
[[ "$protected_code" == 401 || "$protected_code" == 403 ]] || { echo "protected route must deny anonymous access, got HTTP $protected_code" >&2; exit 1; }
printf 'sha=%s\nhealth=200\nprotected=%s\n' "$EXPECTED_SHA" "$protected_code" >> "$backup_manifest"
echo "deployment succeeded: environment=$ENVIRONMENT sha=$EXPECTED_SHA backup_manifest=$backup_manifest"
