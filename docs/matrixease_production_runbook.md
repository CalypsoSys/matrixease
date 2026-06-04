# MatrixEase production runbook

This runbook covers deploying, validating, rolling back, and maintaining the MatrixEase API stack in the lab.

Related docs:

- [matrixease_web_api_frontend_plan.md](./matrixease_web_api_frontend_plan.md)
- [matrixease_ubuntu_host_preparation.md](./matrixease_ubuntu_host_preparation.md)
- [caddy_host_setup.md](./caddy_host_setup.md)
- [cloudflare-pages-gateway.md](./cloudflare-pages-gateway.md)
- [matrixease_local_vscode.md](./matrixease_local_vscode.md)

## Steady-state topology

Public:

- `app.matrixease.com` on Cloudflare Pages
- `api.matrixease.com` through Cloudflare Tunnel

Private lab origin:

- Caddy routes `api.matrixease.com` to `127.0.0.1:8083`
- Docker runs `matrixease-api`
- MatrixEase data lives under `/srv/stacks/matrixease/data`

## Server layout

Expected structure:

```text
/srv/stacks/matrixease/api
  docker-compose.yml
  config.yaml
  matrixease-api-latest.tar
  scripts/
    compose-matrixease.sh
    matrixease.logrotate

/srv/stacks/matrixease/data
/srv/backups/matrixease
/srv/logs/matrixease/api
/srv/utilities/bin/render-config-env
/srv/logs/caddy
```

Server-local files that must not come from Git:

- `/srv/stacks/matrixease/api/config.yaml`
- Cloudflare Tunnel credentials
- real secrets and tokens

## Files from this repo

Copy or derive these from the repo:

- `MatrixEase.Web/Dockerfile`
- `docker/matrixease/docker-compose.yml`
- `scripts/matrixease/compose-matrixease.sh`
- `scripts/matrixease/config.example.yaml`
- `scripts/matrixease/matrixease.logrotate`
- `scripts/caddy/caddy.logrotate`
- the built `matrixease-api` image tarball

## Build the API image locally

From the repo root:

```bash
mkdir -p /mnt/c/transfer
if [ -f /mnt/c/transfer/matrixease-api-latest.tar.gz ]; then mv /mnt/c/transfer/matrixease-api-latest.tar.gz /mnt/c/transfer/matrixease-api-latest.lastgood.tar.gz; fi
docker build --platform linux/amd64 -t matrixease-api:latest -f MatrixEase.Web/Dockerfile .
docker save matrixease-api:latest -o /mnt/c/transfer/matrixease-api-latest.tar
gzip -f /mnt/c/transfer/matrixease-api-latest.tar
```

## Build the YAML renderer

Build the shared renderer in dev/WSL so the server receives a Linux binary. Do not build this on the production host.

```bash
cd /path/to/babalu-yaml-env
mkdir -p /mnt/c/transfer
if [ -f /mnt/c/transfer/render-config-env ]; then mv /mnt/c/transfer/render-config-env /mnt/c/transfer/render-config-env.lastgood; fi
go build -o /mnt/c/transfer/render-config-env ./cmd/babalu_yaml_env
```

Prepare the shared utility directory manually on the Ubuntu host before the first copy:

```bash
sudo mkdir -p /srv/utilities/bin
sudo chown "$USER:$USER" /srv/utilities/bin
chmod 755 /srv/utilities /srv/utilities/bin
```

Then copy from Windows PowerShell:

```powershell
$server = "replace_with_user@replace_with_server"
scp C:\transfer\render-config-env ${server}:/srv/utilities/bin/render-config-env
```

## Create production config

On the Ubuntu host:

```bash
cd /srv/stacks/matrixease/api
vi config.yaml
chmod 600 config.yaml
```

Minimum production values:

```yaml
MATRIXEASE_API_IMAGE: matrixease-api:latest

ASPNETCORE_ENVIRONMENT: Production
MATRIXEASE_API_HOST_BIND: 127.0.0.1
MATRIXEASE_API_HOST_PORT: 8083
MATRIXEASE_DATA_HOST_PATH: /srv/stacks/matrixease/data
MATRIXEASE_LOGS_HOST_PATH: /srv/logs/matrixease/api

MatrixEase:
  Web:
    AccessLogPath: /app/logs/access.log
    ErrorLogPath: /app/logs/errors.log
    FileSaveLocation: /app/data
    FrontendBaseUrl: https://app.matrixease.com
    AllowedOrigins:
      - https://app.matrixease.com
    RequireGatewaySecret: true
    GatewaySecretHeaderName: X-Internal-Api-Key
    GatewaySecret: ${MATRIXEASE_GATEWAY_SECRET}
    RateLimit:
      Enabled: true
      PermitLimit: 120
      WindowSeconds: 60
      QueueLimit: 0
    ProtectionKey: ${MATRIXEASE_PROTECTION_KEY}
    SupabaseUrl: ${MATRIXEASE_SUPABASE_URL}
    SupabaseAnonKey: ${MATRIXEASE_SUPABASE_PUBLISHABLE_KEY}
    SupabaseJwtSecret: ${MATRIXEASE_SUPABASE_JWT_SECRET}
    SlackFeedbackWebhookUrl: ${MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL}
    MaxConcurrentJobs: 10
```

## Deploy

Copy the built files to `/srv/stacks/matrixease/api`, then run:

```bash
cd /srv/stacks/matrixease/api
docker load < matrixease-api-latest.tar
gzip -dc matrixease-api-latest.tar.gz | docker load
chmod +x scripts/compose-matrixease.sh
chmod 755 /srv/utilities/bin/render-config-env
test -x /srv/utilities/bin/render-config-env && echo "render utility present"
scripts/compose-matrixease.sh config
scripts/compose-matrixease.sh up -d
scripts/compose-matrixease.sh ps
```

Use only one image-load command depending on whether the transferred image is compressed or uncompressed.

## Validate

Check the container:

```bash
docker ps --filter name=matrixease-api
docker logs --tail 100 matrixease-api
```

Check direct API port:

```bash
curl -i http://127.0.0.1:8083/
```

Check through Caddy:

```bash
curl -i -H "Host: api.matrixease.com" http://127.0.0.1:80/
```

Check gateway-secret enforcement on a protected route:

```bash
curl -i -H "Host: api.matrixease.com" http://127.0.0.1:80/api/feedback/save_message/
curl -i -H "Host: api.matrixease.com" -H "X-Internal-Api-Key: $MATRIXEASE_GATEWAY_SECRET" http://127.0.0.1:80/api/feedback/save_message/
```

The first request should return `401` in production. The second should reach the application; a `405` is expected for
that `GET` request because the feedback endpoint accepts `POST`.

Check logs:

```bash
ls -l /srv/logs/matrixease/api
tail -n 50 /srv/logs/matrixease/api/access.log
tail -n 50 /srv/logs/matrixease/api/errors.log
```

Feedback posts to Slack only. If `MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL` is missing, the endpoint returns a
configuration failure instead of falling back to email.

## Rollback

Keep the previous image tarball as:

```text
matrixease-api-latest.lastgood.tar.gz
```

Rollback:

```bash
cd /srv/stacks/matrixease/api
scripts/compose-matrixease.sh down
gzip -dc matrixease-api-latest.lastgood.tar.gz | docker load
scripts/compose-matrixease.sh up -d
scripts/compose-matrixease.sh ps
```

## Maintenance

Rotate logs through host logrotate:

```bash
sudo logrotate -d /etc/logrotate.d/matrixease-api
sudo logrotate -d /etc/logrotate.d/caddy
```

Back up file storage:

```bash
sudo tar -czf /srv/backups/matrixease/matrixease-data-$(date +%Y%m%d).tar.gz -C /srv/stacks/matrixease data
```

Keep backup retention and encryption policy outside this phase until storage hardening is designed.
