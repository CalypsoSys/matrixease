# shared.inctrak.com local VS Code launch

Use this flow when you want the repo to run like the MMA split:

- `frontend/` as the local Vite app
- `shared.inctrak.com/` as the API-only ASP.NET Core backend
- local configuration rendered into `AppSettings__...` environment variables

## Local config file

Create:

```text
scripts/shared.inctrak.com/config.local.yaml
```

You can start from:

```text
scripts/shared.inctrak.com/config.example.yaml
```

This local file is gitignored.

The shared API now expects feedback notifications through:

```text
AppSettings__SlackFeedbackWebhookUrl
```

Access logging is also env-driven now through:

```text
AppSettings__AccessLogPath
```

Rate limiting follows the MMA nested config shape:

```text
AppSettings__RateLimit__Enabled
AppSettings__RateLimit__PermitLimit
AppSettings__RateLimit__WindowSeconds
AppSettings__RateLimit__QueueLimit
```

Email-code sign-in is deprecated in this split backend and is no longer configured through SMTP or SendGrid settings.

## How launch works

The backend launch entries in `.vscode/launch.json` use the `backend: prepare local launch` task.

That task:

1. renders `scripts/shared.inctrak.com/config.local.yaml`
2. writes flattened env vars to `.vscode/shared.inctrak-api.env`
3. builds `shared.inctrak.com`
4. launches the API with that env file

The frontend launch uses Vite on `http://localhost:3000` and proxies:

- `/api/*`
- `/google/*`
- `/account/*`
- `/signin-google`

to the backend on `http://localhost:5000`.

## Prerequisites

One of these must be available locally:

- a working Go toolchain so `go run ./cmd/render-shared-inctrak-env ...` succeeds
- a prebuilt `scripts/shared.inctrak.com/render-shared-inctrak-env` binary
