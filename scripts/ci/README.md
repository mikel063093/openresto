# Gated CI/CD bootstrap

`ci.yml` is the deployment authority:

- PRs targeting `develop` or `release` run on GitHub-hosted runners. They never
  use the privileged deployment runner.
- The workflow fails unless frontend and deployable backend API **total line
  coverage are both at least 80%**, and it also runs backend, PostgreSQL,
  Docker/ZAP and Playwright gates.
- A successful push to `develop` runs the isolated `test` deployment job.
- A successful push to `release` runs the `production` deployment job.

## GitHub configuration

Create repository environments named `test` and `production`, with deployment
branch policies limited to `develop` and `release` respectively. The deployment
runner must be repository-scoped and carry all labels:

```text
self-hosted, linux, openresto-deploy
```

Do not grant PR jobs access to that runner. GitHub-hosted jobs perform all PR
validation; only a push after merge can schedule an environment deployment.

No deployment credential is stored in GitHub Actions secrets. The privileged
runner reads its local files below. This avoids copying VPS/database credentials
into workflow logs or third-party runners.

## Local secret contract

On the runner host, create `/opt/openresto-ci` owned by root with mode `0700`,
then `/opt/openresto-ci/secrets/{test,production}.env`, each root-owned mode
`0600`. Do not commit these files or place values in a GitHub repository variable.

Each file must define, without quotes that would become part of a value:

```dotenv
DEPLOY_COMPOSE_FILES=/absolute/path/to/base.yml:/absolute/path/to/approved-overlay.yml
DEPLOY_PROJECT=exact-compose-project-name
DEPLOY_PROVIDER=sqlite
DEPLOY_SERVICES=backend frontend reverse-proxy
DEPLOY_URL=https://environment.example
DEPLOY_HEALTH_PATH=/api/health
DEPLOY_PROTECTED_PATH=/api-reference
# Optional only when the compose deployment needs a separate root-owned env file:
DEPLOY_ENV_FILE=/absolute/path/to/environment.env
```

For PostgreSQL deployments set `DEPLOY_PROVIDER=postgres` and define
`POSTGRES_BACKUP_USER` / `POSTGRES_BACKUP_PASSWORD` in the same protected file.
The backup role must be a read-only, non-superuser role that can run `pg_dump`.
The application runtime role must not be the bootstrap/migration role.

`DEPLOY_COMPOSE_FILES` must resolve to compose files which build the Actions
checkout or reference immutable images pinned to the workflow SHA. A floating
`latest` image or a `git pull` in a deployment script is prohibited.

For `test`, preserve the isolated `test-rest` volumes/networks and set optional
WhatsApp/n8n/reservation-bot integration flags to disabled unless every real
credential and endpoint has been deliberately provisioned. For production, do
not use `test` volumes, routes, or credentials.

## Backup and deployment behavior

Before every deployment `deploy-environment.sh` verifies Compose config and then:

- PostgreSQL: writes a custom `pg_dump`, a `pg_restore --list` manifest and a
  SHA-256 checksum.
- SQLite: performs SQLite's `.backup` operation, runs `PRAGMA integrity_check`,
  and writes a SHA-256 checksum.

Backup manifests are written below `/opt/openresto-ci/backups/<environment>/`.
The script starts only configured core services from the exact checked-out SHA,
waits for public HTTPS health `200`, and requires the protected route to return
`401` or `403` anonymously. On an error it emits bounded Compose logs and stops.

It **does not automatically roll back** after a failed post-start verification:
a schema/data migration may already have made a blind rollback unsafe. Operators
must inspect the manifest, logs and migration state, then either deploy the
previous known-good immutable SHA or use the documented restore procedure. Never
replace/delete persistent volumes as a recovery shortcut.

## Manual verification

Before enabling a production release merge, run from a clean checked-out SHA on
the runner host:

```bash
scripts/ci/deploy-environment.sh --environment test --sha "$(git rev-parse HEAD)"
```

Production uses the same command with `--environment production`, but only after
an explicit production change approval and a confirmed backup manifest. The
workflow intentionally has no `workflow_dispatch` deployment escape hatch.
