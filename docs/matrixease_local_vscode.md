# MatrixEase local VS Code launch

Use this flow to run `MatrixEase.Web` locally with the same YAML-to-environment configuration shape planned for Docker
deployment.

## Local config file

The local source of truth is:

```text
scripts/matrixease/config.local.yaml
```

Start from:

```text
scripts/matrixease/config.example.yaml
```

`config.local.yaml` is gitignored. Fill in real values directly or export the referenced environment variables before
launch.

Required local secret inputs:

```bash
export MATRIXEASE_GATEWAY_SECRET=replace-with-a-local-internal-key
export MATRIXEASE_PROTECTION_KEY=replace-with-a-long-random-secret
export MATRIXEASE_SUPABASE_URL=https://your-project-ref.supabase.co
export MATRIXEASE_SUPABASE_PUBLISHABLE_KEY=sb_publishable_replace_me
export MATRIXEASE_SUPABASE_JWT_SECRET=
export MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL=https://hooks.slack.com/services/your/webhook
```

Use an empty `MATRIXEASE_SUPABASE_JWT_SECRET` unless a later backend change explicitly needs the legacy HS256 fallback.

For local file storage, set `MatrixEase.Web.FileSaveLocation` in `config.local.yaml` to a writable path such as:

```yaml
MatrixEase:
  Web:
    FileSaveLocation: /tmp/matrixease-data
```

## Shared renderer

The VS Code task expects an executable renderer at:

```text
scripts/matrixease/render-config-env
```

Build it from the shared repo:

```bash
cd ~/work/calypsosys-workbench/repos/babalu-yaml-env
go build -o ~/work/calypsosys-workbench/repos/matrixease/scripts/matrixease/render-config-env ./cmd/babalu_yaml_env
```

## How VS Code launch works

The backend launch entry in `.vscode/launch.json` uses the `backend: prepare local launch` task.

That task:

1. renders `scripts/matrixease/config.local.yaml`
2. writes flattened environment variables to `.vscode/matrixease-api.env`
3. builds `MatrixEase.Web` with `SkipWebBlaster=true`
4. launches the backend with `.vscode/matrixease-api.env`

The renderer preserves nested config names:

- `MatrixEase.Web.FileSaveLocation` becomes `MatrixEase__Web__FileSaveLocation`
- `MatrixEase.Web.AllowedOrigins[0]` becomes `MatrixEase__Web__AllowedOrigins__0`
- `MatrixEase.Web.RateLimit.Enabled` becomes `MatrixEase__Web__RateLimit__Enabled`

The backend now reads the same config for CORS, Supabase bearer-token validation, gateway-secret enforcement, file logs,
rate limiting, and Slack feedback. Local development leaves `RequireGatewaySecret` off by default.

## Launch entries

Use:

```text
Local: frontend + backend
```

This starts `MatrixEase.Web` and the Vite frontend together. The frontend runs at:

```text
http://127.0.0.1:5173
```

The backend still runs on:

```text
http://localhost:5000
https://localhost:5001
```

For backend-only diagnostics, use:

```text
Local: backend
```

The hidden launch entries are:

```text
Backend: MatrixEase.Web (no browser)
Frontend: Vite
```

Run `corepack pnpm install` from `frontend/` before the first frontend launch.

## Notes

- The backend build skips `web_blaster` because the web path is moving to a static `frontend/` app.
- The Electron app is out of scope for this phase and still has its existing launch entry.
- Keep `.vscode/matrixease-api.env` out of Git; it may contain secrets.

## Backend verification

Run the backend-focused tests with:

```bash
dotnet test MatrixEase.Web.Tests/MatrixEase.Web.Tests.csproj /property:SkipWebBlaster=true
```
