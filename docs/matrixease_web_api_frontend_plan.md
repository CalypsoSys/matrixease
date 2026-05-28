# MatrixEase Web/API/Frontend Modernization Plan

This document records the agreed MatrixEase web modernization target so implementation can restart cleanly from
`master` without carrying over the earlier `shared.inctrak.com` experiment.

## Goals

- Replace the legacy `static.matrixease.wwwroot` web experience with a modern static frontend in `frontend/`.
- Keep `MatrixEase.Web` as the API-only backend project and deploy it at `api.matrixease.com`.
- Run the API in Joe's lab behind host-installed Caddy, matching the IncTrak, MMA, and Bob deployment style.
- Use Cloudflare Pages for the static frontend at `app.matrixease.com`.
- Use Supabase for browser login and API authentication.
- Use YAML-based configuration rendered by the shared `babalu_yaml_env` tool.
- Preserve MatrixEase file-based storage; do not add a database in phase 1.
- Send feedback to Slack only.
- Leave the Electron app for a later phase.
- Do not touch `docs.matrixease.com/` or `www.matrixease.com/` as part of the web app split.

## Locked Decisions

| Topic | Decision |
| --- | --- |
| Static app hostname | `app.matrixease.com` |
| API hostname | `api.matrixease.com` |
| API lab port | `8083` |
| Backend project | Keep `MatrixEase.Web`, convert it to API-only |
| Frontend project | Create `frontend/` |
| Frontend hosting | Cloudflare Pages |
| API ingress | Cloudflare Tunnel -> host Caddy -> `127.0.0.1:8083` |
| Auth | Supabase replaces email-code login |
| Google Sheets import | Phase 2 |
| Google auth direction | Eventually via Supabase |
| Existing user migration | No migration required |
| File storage host path | `/srv/stacks/matrixease/data` |
| Feedback | Slack only |
| Database | None in phase 1 |
| Electron | Do not solve in phase 1 |
| Static site roots | Do not touch `docs.matrixease.com/` or `www.matrixease.com/` |

## Reference Repos

Use these repos as implementation references, not copy-paste sources:

- `repos/inctrak_options`
  - `frontend/` for Vue 3, TypeScript, Vite, Pinia, PrimeVue, Tailwind, Supabase login patterns.
  - `IncTrak.Api` for Supabase token validation, gateway secret, CORS, rate limiting, access/error logging, Slack feedback.
  - `scripts/inctrak/`, `docker/inctrak/`, and `docs/` for YAML config, compose wrapper, Caddy, logrotate, local VS Code, and production runbook patterns.
- `repos/mma`
  - `frontend/` and `mmastatmaster.com` for the app-plus-API split and Caddy/lab deployment style.
  - `scripts/ommadb/` for `babalu_yaml_env`-based config rendering and compose wrapper style.
- `repos/BobNetEverywhere`
  - docs for a simpler web/API/Electron separation and reverse proxy notes.
- `repos/babalu-yaml-env`
  - source of truth for the shared YAML-to-env renderer.

## Target Topology

Recommended steady state:

```text
browser
  -> https://app.matrixease.com
  -> Cloudflare Pages static frontend
  -> /api/* through Pages Functions gateway
  -> https://api.matrixease.com
  -> Cloudflare Tunnel
  -> host-installed Caddy
  -> http://127.0.0.1:8083
  -> Docker container running MatrixEase.Web
  -> /srv/stacks/matrixease/data mounted into the container
```

The API should also remain directly reachable on `127.0.0.1:8083` on the lab host for diagnostics.

## Proposed Repository Shape

```text
frontend/
  package.json
  src/
  tests/
  vite.config.ts

MatrixEase.Web/
  Dockerfile
  Controllers/
  Middleware/
  Common/
  MatrixEase.Web.csproj

scripts/
  matrixease/
    compose-matrixease.sh
    config.example.yaml
    matrixease.logrotate
  caddy/
    caddy.logrotate

docker/
  matrixease/
    docker-compose.yml

docs/
  cloudflare-pages-gateway.md
  caddy_host_setup.md
  matrixease_local_vscode.md
  matrixease_production_runbook.md
  matrixease_ubuntu_host_preparation.md
  matrixease_web_api_frontend_plan.md
```

`www.matrixease.com/` and `docs.matrixease.com/` stay separate static site roots and should not be modified by this
work except for a future explicit request.

## Phase 1 Scope

Phase 1 creates the production shape and ports the core web workflow.

### Backend

- Keep `MatrixEase.Web` as the backend project.
- Convert `MatrixEase.Web` to API-only for hosted web use.
- Remove web-host dependency on `static.matrixease.wwwroot` and `web_blaster`.
- Keep `MatrixEase.Manga` as the processing/file-storage library.
- Bind API configuration from flattened environment variables rendered from YAML. The current backend uses
  `MatrixEase__Web__...` names, so phase 1 should extend that section unless a later refactor deliberately changes
  the config root.
- Add config-driven CORS for:
  - `https://app.matrixease.com`
  - local Vite origin, likely `http://127.0.0.1:5173` or the chosen MatrixEase frontend port
- Add forwarded header support for Caddy/Cloudflare Tunnel.
- Add gateway-secret enforcement for production API routes using:
  - `MATRIXEASE_GATEWAY_SECRET`
  - `AppSettings.GatewaySecretHeaderName`, default `X-Internal-Api-Key`
- Add fixed-window rate limiting like IncTrak/MMA.
- Add access logging and error logging to host-mounted files.
- Add Slack-only feedback notification using:
  - `MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL`
- Add Supabase bearer-token validation using:
  - `MATRIXEASE_SUPABASE_URL`
  - `MATRIXEASE_SUPABASE_PUBLISHABLE_KEY`
  - optional future `MATRIXEASE_SUPABASE_JWT_SECRET` fallback if needed
- Replace email-code login with Supabase identity.
- Use the Supabase user id as the new file-storage user identity.
- Do not migrate existing user catalogs in phase 1.
- Leave Google Sheets import out of phase 1.

Current branch progress:

- Config-driven CORS, forwarded headers, fixed-window rate limiting, access/error file logs, gateway-secret middleware,
  Supabase bearer-token validation, and Slack-only feedback are in place.
- Backend coverage lives in `MatrixEase.Web.Tests`.
- Legacy Google/email-code controllers have been removed from `MatrixEase.Web`; Google Sheets import belongs in the later
  Supabase-provider workflow.

### Frontend

- Create `frontend/` with the same general stack as IncTrak/MMA:
  - Vue 3
  - TypeScript
  - Vite
  - Pinia
  - PrimeVue
  - Tailwind
  - Vitest
  - pnpm/corepack
- Implement Supabase login/logout/session handling.
- Add an API client that uses `/api/*` in the browser so Cloudflare Pages Functions can proxy production requests.
- Port the core MatrixEase app workflow:
  - login
  - upload CSV/file data
  - processing/status polling
  - project/catalog list
  - matrix viewer
  - filters
  - bucketization
  - measures
  - charts
  - dependencies
  - reports
  - CSV export
  - delete matrix
  - feedback/contact
- Keep the old static files as behavior references only while porting. The end state is `frontend/`, not global Vue 2 scripts.

### Docker, Config, and Ops

- Add `scripts/matrixease/config.example.yaml`.
- Ignore `scripts/matrixease/config.local.yaml`.
- Add `scripts/matrixease/compose-matrixease.sh`.
- Use the shared `babalu_yaml_env` binary at `/srv/utilities/bin/render-config-env`.
- Add `docker/matrixease/docker-compose.yml`.
- Add `MatrixEase.Web/Dockerfile` or replace the root Dockerfile with a MatrixEase.Web-only API image Dockerfile.
- Mount host storage:

```text
/srv/stacks/matrixease/data -> /app/data
/srv/logs/matrixease/api -> /app/logs
```

- Add `scripts/matrixease/matrixease.logrotate` for API access/error logs.
- Add or reuse `scripts/caddy/caddy.logrotate` for Caddy logs.

### Local VS Code

Match the IncTrak/MMA debug shape:

- hidden backend launch: `Backend: MatrixEase.Web (no browser)`
- hidden frontend launch: `Frontend: Vite`
- visible compound launch: `Local: frontend + backend`

Backend launch flow:

1. render `scripts/matrixease/config.local.yaml`
2. write `.vscode/matrixease-api.env`
3. build `MatrixEase.Web`
4. launch backend with `.vscode/matrixease-api.env`

Frontend launch flow:

1. start Vite from `frontend/`
2. open the local app URL
3. proxy `/api/*` to `http://localhost:5000`

### Phase 1 Docs

Add or update:

- `docs/matrixease_local_vscode.md`
- `docs/cloudflare-pages-gateway.md`
- `docs/caddy_host_setup.md`
- `docs/matrixease_ubuntu_host_preparation.md`
- `docs/matrixease_production_runbook.md`

The docs should use `vi` in shell examples and must avoid real secrets, private hostnames beyond the agreed public DNS,
machine-specific values, and credential material.

## Phase 1 Acceptance Criteria

- `frontend/` builds and tests.
- `MatrixEase.Web` builds and backend tests pass.
- Local VS Code compound launch starts frontend and backend together.
- Supabase login works locally with configured development values.
- Authenticated upload/catalog/viewer workflow works against local file storage.
- Feedback posts to Slack when configured.
- Docker compose config renders from YAML and validates.
- API logs write to mounted files.
- Caddy sample routes `api.matrixease.com` to `127.0.0.1:8083`.
- Cloudflare Pages gateway docs explain how `app.matrixease.com` reaches `api.matrixease.com`.
- `docs.matrixease.com/` and `www.matrixease.com/` remain untouched.
- No real secrets, PII, or machine-specific settings are committed.

## Phase 2 Scope

Phase 2 starts only after phase 1 is deployed or at least locally stable.

### Google Sheets via Supabase

- Add Google provider support through Supabase.
- Request the Sheets read scope needed for import.
- Design how the frontend/backend receives or refreshes the Google access token.
- Rebuild the Google Sheet import workflow in the new frontend.
- Keep Google Sheets import on the Supabase-provider path; do not restore the old ASP.NET Google OAuth cookie flow.

### Electron Alignment

- Decide whether `MatrixEase.App` uses:
  - the new `frontend/` build,
  - a separate Electron frontend build,
  - or an Electron-specific local web host wrapper.
- Remove Electron dependency on `web_blaster`.
- Remove Electron dependency on `static.matrixease.wwwroot`.
- Preserve desktop-safe auth and secrets handling.
- Confirm packaging for Windows and Linux after the web path is stable.

### Storage Hardening

- Review file storage under `/srv/stacks/matrixease/data`.
- Decide whether per-user data should be encrypted at rest.
- Decide whether additional metadata/index files are needed.
- Add backup/restore runbook details for file storage.
- Add cleanup/retention guidance for generated data and abandoned uploads.

### Functionality Polish

- Improve the matrix viewer UX after parity is reached.
- Add stronger automated coverage for matrix transforms and viewer state.
- Add richer telemetry or Slack operational notifications if useful.
- Review whether any legacy static assets can be removed after Electron is migrated.

## Out of Scope

- Touching `docs.matrixease.com/`.
- Touching `www.matrixease.com/`.
- Adding a database.
- Migrating existing users or catalogs.
- Implementing Google Sheets import in phase 1.
- Solving Electron in phase 1.
- Copying the abandoned `shared.inctrak.com` branch structure.

## Open Implementation Notes

- Prefer `MatrixEase.Web` as the project name and `api.matrixease.com` as the deployment hostname.
- Keep deployment names MatrixEase-specific:
  - `MATRIXEASE_GATEWAY_SECRET`
  - `MATRIXEASE_SUPABASE_URL`
  - `MATRIXEASE_SUPABASE_PUBLISHABLE_KEY`
  - `MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL`
  - `MATRIXEASE_PROTECTION_KEY`
- Consider `MATRIXEASE_API_IMAGE=matrixease-api:latest`.
- Consider `MATRIXEASE_API_HOST_PORT=8083`.
- Consider `MATRIXEASE_DATA_HOST_PATH=/srv/stacks/matrixease/data`.
- Use tests from the first new code change onward.
