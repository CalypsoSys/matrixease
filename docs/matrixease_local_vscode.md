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
```

Optional local inputs:

```bash
export MATRIXEASE_SUPABASE_JWT_SECRET=
export MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL=https://hooks.slack.com/services/your/webhook
```

MatrixEase uses the remote Supabase project directly in local development. The VS Code frontend tasks map
`MATRIXEASE_SUPABASE_URL` and `MATRIXEASE_SUPABASE_PUBLISHABLE_KEY` into Vite's `VITE_SUPABASE_URL` and
`VITE_SUPABASE_PUBLISHABLE_KEY` variables, and the backend renderer uses the same values for bearer-token validation.

The VS Code backend render task defaults `MATRIXEASE_SUPABASE_JWT_SECRET` and
`MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL` to empty when they are unset. Use an empty
`MATRIXEASE_SUPABASE_JWT_SECRET` unless a later backend change explicitly needs the legacy HS256 fallback.

For local file storage, set `MatrixEase.Web.FileSaveLocation` in `config.local.yaml` to a writable path such as:

```yaml
MatrixEase:
  Web:
    FileSaveLocation: /tmp/matrixease-data
```

## Shared renderer

The VS Code task expects an executable renderer at:

```text
/srv/utilities/bin/render-config-env
```

During migration it can still use the legacy repo-local fallback:

```text
scripts/matrixease/render-config-env
```

Example local install from the shared repo:

```bash
cd ~/work/calypsosys-workbench/repos/babalu-yaml-env
go build -o /tmp/render-config-env ./cmd/babalu_yaml_env
sudo mkdir -p /srv/utilities/bin
sudo chown "$USER:$USER" /srv/utilities/bin
cp /tmp/render-config-env /srv/utilities/bin/render-config-env
chmod 755 /srv/utilities/bin/render-config-env
```

## How VS Code launch works

The backend launch entry in `.vscode/launch.json` uses the `backend: prepare local launch` task.

That task:

1. renders `scripts/matrixease/config.local.yaml`
2. writes flattened environment variables to `.vscode/matrixease-api.env`
3. builds `MatrixEase.Web`
4. launches the backend with `.vscode/matrixease-api.env`

The renderer preserves nested config names:

- `MatrixEase.Web.FileSaveLocation` becomes `MatrixEase__Web__FileSaveLocation`
- `MatrixEase.Web.AllowedOrigins[0]` becomes `MatrixEase__Web__AllowedOrigins__0`
- `MatrixEase.Web.RateLimit.Enabled` becomes `MatrixEase__Web__RateLimit__Enabled`

The backend now reads the same config for CORS, Supabase bearer-token validation, gateway-secret enforcement, file logs,
rate limiting, and Slack feedback. Local development leaves `RequireGatewaySecret` off by default.

## Debugger Notes

The workspace disables C# Hot Reload for local MatrixEase debugging because the C# Dev Kit Edit-and-Continue service can
fail before the app starts if its VS Code server install is missing a debugger dependency. If the same
`ManagedEditAndContinue...System.Threading.Tasks.Extensions` message still appears, set these in VS Code User Settings
and reload the VS Code window:

```json
{
  "csharp.experimental.debug.hotReload": false,
  "csharp.debug.hotReloadOnSave": false
}
```

## Launch entries

Use:

```text
Local: frontend + backend
```

This starts the hidden backend debug launch, starts Vite, waits for the backend to answer on port `5000`, then opens a
browser debug session. The frontend runs at:

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

Useful tasks:

```text
backend: render local env
backend: prepare local launch
frontend: dev
frontend: build
frontend: prepare browser launch
```

Run `corepack pnpm install` from `frontend/` before the first frontend launch if `node_modules/` is missing.

## Notes

- `MatrixEase.Web` no longer runs the legacy `web_blaster` prebuild; the Vite app in `frontend/` is the web UI path.
- The Electron app is out of scope for this phase and still has its existing launch entry.
- Keep `.vscode/matrixease-api.env` out of Git; it may contain secrets.

## Backend verification

Run the backend-focused tests with:

```bash
dotnet test MatrixEase.Web.Tests/MatrixEase.Web.Tests.csproj
```
