# Caddy host setup

This document is the host-installed Caddy setup guide for the MatrixEase API host.

Long-term operational reference belongs in:

- [matrixease_ubuntu_host_preparation.md](./matrixease_ubuntu_host_preparation.md)
- [matrixease_production_runbook.md](./matrixease_production_runbook.md)
- [cloudflare-pages-gateway.md](./cloudflare-pages-gateway.md)

## Goal

Run Caddy on the Ubuntu host as the private reverse proxy behind Cloudflare Tunnel.

Recommended request path:

- browser -> Cloudflare Pages at `app.matrixease.com`
- `/api/*` -> Cloudflare Pages Functions
- Pages Functions -> Cloudflare Tunnel hostname `api.matrixease.com`
- Cloudflare Tunnel -> host-installed Caddy
- Caddy -> `matrixease-api` on `127.0.0.1:8083`

## Recommended server layout

```text
/srv/logs/caddy
  caddy.log

/etc/caddy
  Caddyfile
```

Prepare the host log directory:

```bash
sudo mkdir -p /srv/logs/caddy
sudo chown -R caddy:caddy /srv/logs/caddy
sudo chmod 755 /srv/logs/caddy
```

The repo includes a matching host logrotate policy at:

```text
scripts/caddy/caddy.logrotate
```

## Recommended Caddyfile

For the Cloudflare Tunnel pattern, Caddy only needs to listen on the host:

```caddy
{
    auto_https off

    log {
        output file /srv/logs/caddy/caddy.log
        format console
    }
}

http://api.matrixease.com {
    reverse_proxy 127.0.0.1:8083
}
```

## Start and verify Caddy

Run on the Ubuntu host:

```bash
sudo cp /etc/caddy/Caddyfile /etc/caddy/Caddyfile.dist
sudo vi /etc/caddy/Caddyfile
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl enable --now caddy
sudo systemctl restart caddy
sudo systemctl status caddy --no-pager
```

Confirm logs:

```bash
ls -l /srv/logs/caddy
sudo tail -n 50 /srv/logs/caddy/caddy.log
```

Install logrotate:

```bash
export MATRIXEASE_REPO_ROOT=/absolute/path/to/your/matrixease/checkout
sudo cp "$MATRIXEASE_REPO_ROOT/scripts/caddy/caddy.logrotate" /etc/logrotate.d/matrixease-caddy
sudo chmod 644 /etc/logrotate.d/matrixease-caddy
sudo logrotate -d /etc/logrotate.d/matrixease-caddy
```

Check local routing:

```bash
curl -i -H "Host: api.matrixease.com" http://127.0.0.1:80/
```

## Cloudflare Tunnel relationship

Tunnel ingress should point at the local Caddy listener:

```yaml
ingress:
  - hostname: api.matrixease.com
    service: http://127.0.0.1:80
  - service: http_status:404
```

Caddy then proxies `api.matrixease.com` to `127.0.0.1:8083`.

## Maintenance

After editing `/etc/caddy/Caddyfile`:

```bash
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

If needed:

```bash
sudo systemctl restart caddy
sudo journalctl -u caddy -n 100 --no-pager
```
