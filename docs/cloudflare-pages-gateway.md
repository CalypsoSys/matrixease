# Cloudflare Pages API gateway

MatrixEase phase 1 uses Cloudflare Pages for the static frontend and a lab-hosted API backend.

## Target topology

```text
browser
  -> https://app.matrixease.com
  -> Cloudflare Pages
  -> /api/* through Pages Functions
  -> https://api.matrixease.com
  -> Cloudflare Tunnel
  -> host Caddy
  -> 127.0.0.1:8083
  -> MatrixEase.Web
```

## Frontend

The `frontend/` app calls MatrixEase endpoints through same-origin `/api/*` URLs. That keeps browser code independent
of whether it is running locally through Vite or hosted on Cloudflare Pages.

Recommended Cloudflare Pages settings once `frontend/` exists:

- Root directory: `frontend`
- Build command: `corepack pnpm build`
- Build output directory: `dist`

Required frontend environment variables:

```text
VITE_SUPABASE_URL
VITE_SUPABASE_PUBLISHABLE_KEY
```

The values should come from:

```text
MATRIXEASE_SUPABASE_URL
MATRIXEASE_SUPABASE_PUBLISHABLE_KEY
```

## Pages Functions gateway

The frontend should include Pages Functions handlers that proxy MatrixEase API paths to `api.matrixease.com`.

Required Cloudflare Pages environment variables:

```text
API_BASE_URL=https://api.matrixease.com
INTERNAL_API_KEY=<same value as MATRIXEASE_GATEWAY_SECRET>
GATEWAY_SECRET_HEADER_NAME=X-Internal-Api-Key
```

The gateway should:

- preserve path and query string
- forward request method, headers, and body
- remove or replace the `Host` header
- add `X-Internal-Api-Key` with `INTERNAL_API_KEY`
- return the upstream response body, status, and headers

## Backend expectations

In production, `MatrixEase.Web` should require the gateway secret for API routes.

Relevant YAML config:

```yaml
MatrixEase:
  Web:
    RequireGatewaySecret: true
    GatewaySecretHeaderName: X-Internal-Api-Key
    GatewaySecret: ${MATRIXEASE_GATEWAY_SECRET}
    AllowedOrigins:
      - https://app.matrixease.com
```

## Validation

After deployment, validate:

```bash
curl -i https://app.matrixease.com/
curl -i https://api.matrixease.com/
curl -i -H "X-Internal-Api-Key: $MATRIXEASE_GATEWAY_SECRET" https://api.matrixease.com/api/feedback/get_message/
```

Requests missing the gateway header should fail once backend enforcement is enabled.
