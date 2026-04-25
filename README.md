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

Use `appsettings_sample.json` as a template only. Keep real secrets out of tracked config files.

Recommended storage:

- `dotnet user-secrets` for local development secrets
- environment variables for CI, deployment, and machine-specific overrides
- committed JSON only for non-secret defaults and examples

### Local Secrets

`MatrixEase.Web` supports user-secrets, and `MatrixEase.App` now does as well.

Set local development values like this:

```bash
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:FileSaveLocation" "/tmp/matrixease-web"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:ProtectionKey" "replace-with-a-long-random-secret"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:GoogleClientId" "your-google-client-id"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:GoogleClientSecret" "your-google-client-secret"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:EmailApiKey" "your-sendgrid-api-key"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:EmailFrom" "feedback@example.com"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:SNMPServer" "smtp.example.com"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:SNMPPort" "25"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:SNMPAddress" "smtp-user@example.com"
dotnet user-secrets --project MatrixEase.Web set "MatrixEase:Web:SNMPPassword" "your-smtp-password"
```

```bash
dotnet user-secrets --project MatrixEase.App set "MatrixEase:App:GoogleClientId" "your-google-client-id"
```

Equivalent environment variables:

```bash
export MatrixEase__Web__FileSaveLocation=/tmp/matrixease-web
export MatrixEase__Web__ProtectionKey=replace-with-a-long-random-secret
export MatrixEase__Web__GoogleClientId=your-google-client-id
export MatrixEase__Web__GoogleClientSecret=your-google-client-secret
export MatrixEase__Web__EmailApiKey=your-sendgrid-api-key
export MatrixEase__Web__EmailFrom=feedback@example.com
export MatrixEase__Web__SNMPServer=smtp.example.com
export MatrixEase__Web__SNMPPort=25
export MatrixEase__Web__SNMPAddress=smtp-user@example.com
export MatrixEase__Web__SNMPPassword=your-smtp-password
export MatrixEase__App__GoogleClientId=your-google-client-id
```

`MatrixEase:Web:ProtectionKey` secures MatrixEase cookies and other protected values. Treat it like an application secret and set a long random value through user-secrets or deployment-time environment variables.

### Desktop Google Auth Note

`MatrixEase.App` now uses the installed-app Google OAuth flow with a desktop-safe client configuration, so it only needs `GoogleClientId`. `MatrixEase.Web` still runs server-side OAuth and keeps using both `GoogleClientId` and `GoogleClientSecret`.

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

---
