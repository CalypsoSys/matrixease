# MatrixEase Ubuntu host preparation

This document covers one-time host setup for the MatrixEase API stack.

## Host packages

Install Docker, Caddy, and basic utilities using the host's standard package process. Keep Cloudflare Tunnel
credentials and real secrets outside this repo.

Minimum tools:

```bash
sudo apt update
sudo apt install -y curl ca-certificates docker.io docker-compose-plugin caddy logrotate
```

## Directory layout

Create the MatrixEase stack, data, backup, and log directories:

```bash
sudo mkdir -p /srv/stacks/matrixease/api/scripts
sudo mkdir -p /srv/stacks/matrixease/data
sudo mkdir -p /srv/backups/matrixease
sudo mkdir -p /srv/logs/matrixease/api
sudo mkdir -p /srv/logs/caddy

sudo chown -R $USER:$USER /srv/stacks/matrixease
sudo chown -R $USER:$USER /srv/backups/matrixease
sudo chown -R $USER:$USER /srv/logs/matrixease
sudo chown -R caddy:caddy /srv/logs/caddy
```

The API container mounts:

```text
/srv/stacks/matrixease/data -> /app/data
/srv/logs/matrixease/api -> /app/logs
```

## Server-local config

Production config lives at:

```text
/srv/stacks/matrixease/api/config.yaml
```

Create it from:

```text
scripts/matrixease/config.example.yaml
```

Edit with:

```bash
cd /srv/stacks/matrixease/api
vi config.yaml
chmod 600 config.yaml
```

Do not commit `config.yaml`.

## Required secret inputs

Keep these in the host shell environment, a password manager, or the server-local config file:

| Name | Purpose |
| --- | --- |
| `MATRIXEASE_GATEWAY_SECRET` | Internal API key injected by Cloudflare Pages Functions |
| `MATRIXEASE_PROTECTION_KEY` | Data protection key for MatrixEase protected values |
| `MATRIXEASE_SUPABASE_URL` | Supabase project URL |
| `MATRIXEASE_SUPABASE_PUBLISHABLE_KEY` | Supabase browser-safe publishable key |
| `MATRIXEASE_SUPABASE_JWT_SECRET` | Optional legacy JWT fallback; leave empty unless needed |
| `MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL` | Slack webhook for feedback |

## Shared renderer

Build the shared YAML renderer in dev/WSL so the host receives a Linux binary. Do not build this on the production
host:

```bash
cd ~/work/calypsosys-workbench/repos/babalu-yaml-env
mkdir -p /mnt/c/transfer
go build -o /mnt/c/transfer/render-config-env ./cmd/babalu_yaml_env
```

Prepare the host utility directory manually:

```bash
sudo mkdir -p /srv/utilities/bin
sudo chown "$USER:$USER" /srv/utilities/bin
chmod 755 /srv/utilities /srv/utilities/bin
```

Copy that binary to:

```text
/srv/utilities/bin/render-config-env
```

Then make it executable:

```bash
chmod 755 /srv/utilities/bin/render-config-env
```

## Logrotate

Install logrotate policies:

```bash
export MATRIXEASE_REPO_ROOT=/absolute/path/to/your/matrixease/checkout
sudo cp "$MATRIXEASE_REPO_ROOT/scripts/matrixease/matrixease.logrotate" /etc/logrotate.d/matrixease-api
sudo cp "$MATRIXEASE_REPO_ROOT/scripts/caddy/caddy.logrotate" /etc/logrotate.d/caddy
sudo chmod 644 /etc/logrotate.d/matrixease-api /etc/logrotate.d/caddy
sudo logrotate -d /etc/logrotate.d/matrixease-api
sudo logrotate -d /etc/logrotate.d/caddy
```

## Caddy

Configure Caddy using:

```text
docs/caddy_host_setup.md
```

Expected route:

```text
api.matrixease.com -> 127.0.0.1:8083
```
