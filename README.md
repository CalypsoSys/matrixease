# MatrixEase

**An innovative visualization and analysis tool for large matrix data.**  
MatrixEase helps you see and explore data the way you think—fast, intuitive, and scalable.

---

## Table of Contents

- [Overview](#overview)  
- [Key Features](#key-features)  
- [Quick Start](#quick-start)  
- [Core Concepts & Components](#core-concepts--components)  
- [Screens & Navigation](#screens--navigation)  
- [Configuration & Setup](#configuration--setup)  
- [Development](#development)  
- [Help & Support](#help--support)  
- [Roadmap](#roadmap)  
- [License](#license)  
- [Contact](#contact)

---

## Overview

MatrixEase is designed to help teams visualize and analyze large-scale matrix data with intuitive, interactive interfaces and visual tools.

- **Website**: [matrixease.com](https://www.matrixease.com/)  
- **Documentation**: [docs.matrixease.com](https://docs.matrixease.com/)  
- **Support Portal**: [support.matrixease.com](https://support.matrixease.com/)  
- **Blog**: [blog.matrixease.com](https://blog.matrixease.com/)  
- **User Portal**: [my.matrixease.com](https://my.matrixease.com/)  
- **Product Vision**: “See data the way you think about it — An Innovative Visualization and Analysis Tool for large matrix data.” ([docs.matrixease.com](https://docs.matrixease.com/))  

---

## Key Features

- **Fast, interactive visualization** of large matrices with intuitive dashboards.  
- **Role-based navigation**: tailored access for analysts, admins, and viewers.  
- **Modular components**: explore data via configurable, composable UI components.  
- **Multi-platform deployment**: flexible deployment options aligned to your needs.  
- **Support & learning**: accessible FAQs, tutorials, and video guides.  

---

## Quick Start

Follow the “Quick Start” guide in our documentation to get up and running in minutes:

1. Choose your deployment platform.  
2. Install or launch the application.  
3. Connect your datasets or upload matrix data.  
4. Explore using built-in visualization tools.

Check out the **Quick Start** section in the [docs](https://docs.matrixease.com/) for full instructions and guided examples.

---

## Core Concepts & Components

- **Visualization Components**: building blocks for exploring matrix data—heatmaps, adjacency graphs, and more.  
- **Analytics Tools**: filters, aggregators, and drill-down capabilities to extract insights.  
- **Config & Settings**: customize data views, access rights, thresholds, and more.  
- **Help Resources**: in-app prompts link to tutorials, FAQs, or support.  

---

## Screens & Navigation

Users can navigate across:

- **Dashboard / My MatrixEase** – personal workspace with saved views and recent projects.  
- **Components** – view, configure, and assemble visualization components.  
- **Support & Blog** – access tutorials, FAQs, walkthroughs, and latest updates.  
- **About & Legal** – terms of service, privacy policy, credit information.  

---

## Configuration & Setup

Do not rely on tracked `appsettings.json` files for `shared.inctrak.com`. Use the YAML-to-env flow and keep real secrets out of tracked config files.

Recommended storage:

- `dotnet user-secrets` for local development secrets
- environment variables for CI, deployment, and machine-specific overrides
- committed JSON only for non-secret defaults and examples

### Local Secrets

`shared.inctrak.com` is now the env-first API backend, and `MatrixEase.App` still supports user-secrets for desktop-only settings.

Set local development values like this:

```bash
dotnet user-secrets --project MatrixEase.App set "MatrixEase:App:GoogleClientId" "your-google-client-id"
```

Use a local YAML file for the shared API split:

```bash
cp scripts/shared.inctrak.com/config.example.yaml scripts/shared.inctrak.com/config.local.yaml
```

Populate that file using environment-variable placeholders such as:

```bash
export MATRIXEASE_FILE_ROOT=/tmp/matrixease-web
export MATRIXEASE_PROTECTION_KEY=replace-with-a-long-random-secret
export GOOGLE_CLIENT_ID=your-google-client-id
export GOOGLE_CLIENT_SECRET=your-google-client-secret
export SHARED_INCTRAK_SLACK_FEEDBACK_WEBHOOK_URL=https://hooks.slack.com/services/your/webhook
export SHARED_INCTRAK_ACCESS_LOG_PATH=logs/access.log
export SHARED_INCTRAK_GATEWAY_SECRET=replace-with-an-internal-api-key
export MatrixEase__App__GoogleClientId=your-google-client-id
```

`AppSettings__ProtectionKey` secures MatrixEase cookies and other protected values. Treat it like an application secret and set a long random value through local env files or deployment-time environment variables.

### Desktop Google Auth Note

`MatrixEase.App` uses the installed-app Google OAuth flow with a desktop-safe client configuration, so it only needs `GoogleClientId`. `shared.inctrak.com` still runs server-side OAuth and uses both `GoogleClientId` and `GoogleClientSecret`.

---

## Development

Use the VS Code launch configuration or the root `Makefile` for the common development tasks:

```bash
make restore
make build
make test
make run-web
```

Useful variants:

- `make build-app`
- `make build-web`
- `make build-manga`
- `make build-tester`
- `make build-web-blaster`
- `make docker-up`
- `make CONFIGURATION=Release build`

The `Makefile` replaces the old root solution file and keeps the repo entry points project-scoped.

## Secret Scanning

This repo now includes a standard `gitleaks` setup:

- repo config in `.gitleaks.toml`
- pre-commit hook config in `.pre-commit-config.yaml`
- local runner via `make gitleaks`
- GitHub Actions workflow in `.github/workflows/gitleaks.yml`

Run it locally with:

```bash
make gitleaks
```

To enable the local pre-commit hook:

```bash
pip install pre-commit
make install-hooks
```

After that, `gitleaks` will run automatically on each commit through `pre-commit`, and the GitHub Actions workflow will enforce the same scan in CI.

If you already use `make secrets`, it remains available as an alias for backward compatibility.

If you want a local JSON report for triage, run:

```bash
gitleaks detect --source . --config .gitleaks.toml --no-banner --redact --report-format json --report-path .gitleaks-report.json
```

The initial config keeps the default `gitleaks` rules and only allows a few explicit placeholder values used in docs and sample setup text. If the first real scan finds historical false positives, we should review those together before adding any broader allowlist or baseline.

---
