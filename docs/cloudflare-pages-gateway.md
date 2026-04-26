# Cloudflare Pages API gateway

This repo now supports the MMA-style split:

- `frontend/` builds as the frontend site
- `shared.inctrak.com/` runs as the upstream ASP.NET Core API
- Cloudflare Pages Functions proxy frontend-origin requests to the API

## Local development

Vite proxies these paths to `VITE_API_PROXY_TARGET`:

- `/api/*`
- `/google/*`
- `/account/*`
- `/signin-google`

The default target is:

```text
http://localhost:5000
```

## Cloudflare Pages

The Pages Functions entrypoints live under:

```text
frontend/functions/
```

Recommended Pages build settings:

- Root directory: `frontend`
- Framework preset: `None`
- Build command: `npm ci && npm run build`
- Build output directory: `dist`

Required environment variables:

- `API_BASE_URL`
- `INTERNAL_API_KEY`

Optional:

- `GATEWAY_SECRET_HEADER_NAME`

The proxy forwards requests upstream and adds the internal API key using
`GATEWAY_SECRET_HEADER_NAME` or `X-Internal-Api-Key` by default.
